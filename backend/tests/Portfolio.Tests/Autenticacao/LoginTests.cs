using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Core.Autenticacao;
using Portfolio.Tests.BaseDeDados;
using RespostaLogin = Portfolio.Api.Autenticacao.RespostaLogin;

namespace Portfolio.Tests.Autenticacao;

// Login pela API completa, com a base de dados real (docs/05-plano-implementacao.md, fase 2).
[Collection("BaseDeDados")]
public sealed class LoginTests(PostgresFixture bd) : IAsyncLifetime
{
    private const string Login = "/api/backoffice/auth/login";
    private const string DoisFatores = "/api/backoffice/auth/2fa";
    private const string Eu = "/api/backoffice/auth/eu";

    private FabricaApi _api = null!;

    public async ValueTask InitializeAsync()
    {
        bd.Exigir();
        await using (var contexto = bd.NovoBackoffice())
            await contexto.Database.ExecuteSqlRawAsync("DELETE FROM \"Utilizador\";", TestContext.Current.CancellationToken);

        _api = new FabricaApi(bd);
        await _api.CriarAdminAsync();
    }

    public async ValueTask DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task Password_errada_e_email_inexistente_devolvem_a_mesma_resposta()
    {
        var cliente = await _api.NovoClienteComCsrfAsync();

        var passwordErrada = await _api.LoginAsync(cliente, password: "password-errada-123");
        var emailInexistente = await _api.LoginAsync(cliente, email: "outro@exemplo.pt");

        Assert.Equal(HttpStatusCode.Unauthorized, passwordErrada.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, emailInexistente.StatusCode);
        Assert.Equal("Email ou password incorretos", await Titulo(passwordErrada));
        Assert.Equal("Email ou password incorretos", await Titulo(emailInexistente));
    }

    [Fact]
    public async Task O_email_nao_distingue_maiusculas()
    {
        var cliente = await _api.NovoClienteComCsrfAsync();

        var resposta = await _api.LoginAsync(cliente, email: "ADMIN@Exemplo.pt");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Sem_sessao_o_backoffice_responde_401_em_problem_details()
    {
        var cliente = _api.NovoCliente();

        var resposta = await cliente.GetAsync(Eu);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Sem_2fa_a_sessao_parcial_nao_acede_ao_backoffice()
    {
        var cliente = await _api.NovoClienteComCsrfAsync();

        var login = await _api.LoginAsync(cliente);
        var eu = await cliente.GetAsync(Eu);

        Assert.Equal("configurar-2fa", (await login.Content.ReadFromJsonAsync<RespostaLogin>(Ct))!.ProximoPasso);
        Assert.Equal(HttpStatusCode.Forbidden, eu.StatusCode);
    }

    [Fact]
    public async Task No_primeiro_login_configura_o_2fa_e_abre_a_sessao_completa()
    {
        var cliente = await _api.NovoClienteComCsrfAsync();
        await _api.LoginAsync(cliente);

        var configuracao = await cliente.PostAsync(DoisFatores + "/configurar");
        var dados = await configuracao.Content.ReadFromJsonAsync<ConfiguracaoTotp>(Ct);
        var codigoErrado = await cliente.PostAsync(DoisFatores + "/ativar", new { codigo = "000000" });
        var ativacao = await cliente.PostAsync(DoisFatores + "/ativar", new { codigo = _api.Codigo(dados!.Segredo) });
        var codigos = await ativacao.Content.ReadFromJsonAsync<FabricaApi.RespostaCodigos>(Ct);
        var eu = await cliente.GetAsync(Eu);

        Assert.StartsWith("data:image/png;base64,", dados.QrCode);
        Assert.StartsWith("otpauth://totp/", dados.Uri);
        Assert.Equal(HttpStatusCode.Unauthorized, codigoErrado.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ativacao.StatusCode);
        Assert.Equal(10, codigos!.CodigosRecuperacao.Distinct().Count());
        Assert.Equal(HttpStatusCode.OK, eu.StatusCode);
    }

    [Fact]
    public async Task Com_o_2fa_ativo_o_login_pede_o_codigo()
    {
        var (segredo, _) = await _api.EntrarComDoisFatoresAsync(await _api.NovoClienteComCsrfAsync());
        _api.AvancarParaProximoCodigo();
        var cliente = await _api.NovoClienteComCsrfAsync();

        var login = await _api.LoginAsync(cliente);
        var configurar = await cliente.PostAsync(DoisFatores + "/configurar");
        var codigo = await cliente.PostAsync(DoisFatores, new { codigo = _api.Codigo(segredo) });
        var eu = await cliente.GetAsync(Eu);

        Assert.Equal("codigo", (await login.Content.ReadFromJsonAsync<RespostaLogin>(Ct))!.ProximoPasso);
        Assert.Equal(HttpStatusCode.Conflict, configurar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, codigo.StatusCode);
        Assert.Equal(HttpStatusCode.OK, eu.StatusCode);
    }

    [Fact]
    public async Task Um_codigo_2fa_reutilizado_e_recusado()
    {
        var (segredo, _) = await _api.EntrarComDoisFatoresAsync(await _api.NovoClienteComCsrfAsync());
        var cliente = await _api.NovoClienteComCsrfAsync();
        await _api.LoginAsync(cliente);

        // O mesmo intervalo de 30 s que serviu para ativar o 2FA.
        var resposta = await cliente.PostAsync(DoisFatores, new { codigo = _api.Codigo(segredo) });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Um_codigo_de_recuperacao_so_serve_uma_vez()
    {
        var (_, codigos) = await _api.EntrarComDoisFatoresAsync(await _api.NovoClienteComCsrfAsync());

        var primeiro = await _api.NovoClienteComCsrfAsync();
        await _api.LoginAsync(primeiro);
        var usado = await primeiro.PostAsync(DoisFatores, new { codigo = codigos[0].ToLowerInvariant() });

        var segundo = await _api.NovoClienteComCsrfAsync();
        await _api.LoginAsync(segundo);
        var reutilizado = await segundo.PostAsync(DoisFatores, new { codigo = codigos[0] });

        Assert.Equal(HttpStatusCode.OK, usado.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reutilizado.StatusCode);
    }

    [Fact]
    public async Task Cinco_codigos_errados_obrigam_a_recomecar_pela_password()
    {
        var (segredo, _) = await _api.EntrarComDoisFatoresAsync(await _api.NovoClienteComCsrfAsync());
        _api.AvancarParaProximoCodigo();
        var cliente = await _api.NovoClienteComCsrfAsync();
        await _api.LoginAsync(cliente);

        HttpResponseMessage ultima = null!;
        for (var i = 0; i < 5; i++)
            ultima = await cliente.PostAsync(DoisFatores, new { codigo = "000000" });
        var codigoCerto = await cliente.PostAsync(DoisFatores, new { codigo = _api.Codigo(segredo) });

        Assert.Contains("Volte a introduzir a password", await Titulo(ultima));
        Assert.Equal(HttpStatusCode.Unauthorized, codigoCerto.StatusCode);
    }

    [Fact]
    public async Task A_conta_bloqueia_depois_de_5_falhas()
    {
        var cliente = await _api.NovoClienteComCsrfAsync();
        for (var i = 0; i < 5; i++)
            await _api.LoginAsync(cliente, password: "password-errada-123");

        var bloqueado = await _api.LoginAsync(cliente);
        _api.Relogio.Advance(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(1));
        var desbloqueado = await _api.LoginAsync(cliente);

        // Durante o bloqueio, a resposta é a mesma de uma password errada (SEG04).
        Assert.Equal(HttpStatusCode.Unauthorized, bloqueado.StatusCode);
        Assert.Equal("Email ou password incorretos", await Titulo(bloqueado));
        Assert.Equal(HttpStatusCode.OK, desbloqueado.StatusCode);
    }

    [Fact]
    public async Task O_logout_invalida_a_sessao_no_servidor()
    {
        var cliente = await _api.NovoClienteComCsrfAsync();
        await _api.EntrarComDoisFatoresAsync(cliente);
        var copia = cliente.CopiarSessaoPara(_api.NovoCliente());

        var logout = await cliente.PostAsync("/api/backoffice/auth/logout");
        var euComCookieAntigo = await copia.GetAsync(Eu);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, euComCookieAntigo.StatusCode);
    }

    [Fact]
    public async Task A_sessao_expira_ao_fim_de_30_minutos_sem_atividade()
    {
        var cliente = await _api.NovoClienteComCsrfAsync();
        await _api.EntrarComDoisFatoresAsync(cliente);

        _api.Relogio.Advance(TimeSpan.FromMinutes(20));
        var comAtividade = await cliente.GetAsync(Eu);
        _api.Relogio.Advance(TimeSpan.FromMinutes(20));
        var aindaAtiva = await cliente.GetAsync(Eu);
        _api.Relogio.Advance(TimeSpan.FromMinutes(31));
        var expirada = await cliente.GetAsync(Eu);

        Assert.Equal(HttpStatusCode.OK, comAtividade.StatusCode);
        Assert.Equal(HttpStatusCode.OK, aindaAtiva.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, expirada.StatusCode);
    }

    [Fact]
    public async Task A_sessao_termina_ao_fim_de_8_horas_mesmo_com_atividade()
    {
        var cliente = await _api.NovoClienteComCsrfAsync();
        await _api.EntrarComDoisFatoresAsync(cliente);

        HttpResponseMessage resposta;
        var decorrido = TimeSpan.Zero;
        do
        {
            _api.Relogio.Advance(TimeSpan.FromMinutes(20));
            decorrido += TimeSpan.FromMinutes(20);
            resposta = await cliente.GetAsync(Eu);
        }
        while (resposta.StatusCode == HttpStatusCode.OK && decorrido < TimeSpan.FromHours(9));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal(TimeSpan.FromHours(8), decorrido);
    }

    [Fact]
    public async Task A_sessao_parcial_expira_ao_fim_de_5_minutos()
    {
        var (segredo, _) = await _api.EntrarComDoisFatoresAsync(await _api.NovoClienteComCsrfAsync());
        var cliente = await _api.NovoClienteComCsrfAsync();
        await _api.LoginAsync(cliente);

        _api.Relogio.Advance(TimeSpan.FromMinutes(6));
        var resposta = await cliente.PostAsync(DoisFatores, new { codigo = _api.Codigo(segredo) });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Um_pedido_de_escrita_sem_token_csrf_e_recusado()
    {
        var cliente = await _api.NovoClienteComCsrfAsync();

        var resposta = await cliente.EnviarAsync(HttpMethod.Post, Login, new { email = FabricaApi.Email, password = FabricaApi.Password }, comCsrf: false);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task O_cookie_de_sessao_e_httponly_secure_e_samesite_strict()
    {
        var cliente = await _api.NovoClienteComCsrfAsync();

        var resposta = await _api.LoginAsync(cliente);

        var cookie = resposta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("portfolio.sessao=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task As_respostas_levam_os_cabecalhos_de_seguranca()
    {
        var resposta = await _api.NovoCliente().GetAsync("/api/backoffice/auth/csrf");

        Assert.Equal("nosniff", resposta.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", resposta.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", resposta.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("frame-ancestors 'none'", resposta.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task O_rate_limit_trava_os_logins_do_mesmo_ip()
    {
        await using var api = new FabricaApi(bd, new() { ["Autenticacao:LoginsPorIp"] = "2" });
        var cliente = await api.NovoClienteComCsrfAsync();

        await api.LoginAsync(cliente, password: "password-errada-123");
        await api.LoginAsync(cliente, password: "password-errada-123");
        var terceira = await api.LoginAsync(cliente);

        Assert.Equal(HttpStatusCode.TooManyRequests, terceira.StatusCode);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<string?> Titulo(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<ProblemDetails>(Ct))?.Title;
}
