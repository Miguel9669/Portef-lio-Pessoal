using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Conteudo;
using Portfolio.Api.Projetos;
using Portfolio.Tests.Autenticacao;
using Portfolio.Tests.BaseDeDados;

namespace Portfolio.Tests.Conteudo;

// Blocos, projetos e pré-visualização pela API completa, com login e 2FA (fase 3).
[Collection("BaseDeDados")]
public sealed class BackofficeConteudoTests(PostgresFixture bd) : IAsyncLifetime
{
    private const string Blocos = "/api/backoffice/blocos";
    private const string Projetos = "/api/backoffice/projetos";

    private FabricaApi _api = null!;
    private ClienteTeste _cliente = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        bd.Exigir();
        await using (var contexto = bd.NovoBackoffice())
        {
            await contexto.Database.ExecuteSqlRawAsync(
                "DELETE FROM \"Projeto\"; DELETE FROM \"BlocoConteudo\"; DELETE FROM \"Utilizador\";", Ct);
        }

        _api = new FabricaApi(bd);
        await _api.CriarAdminAsync();
        _cliente = await _api.NovoClienteComCsrfAsync();
        await _api.EntrarComDoisFatoresAsync(_cliente);
    }

    public async ValueTask DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task Sem_login_os_blocos_e_projetos_nao_estao_acessiveis()
    {
        var anonimo = _api.NovoCliente();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Blocos)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(Projetos)).StatusCode);
    }

    [Fact]
    public async Task Cria_edita_e_remove_um_bloco()
    {
        var criado = await Ler<RespostaBloco>(await _cliente.PostAsync(Blocos, new { titulo = "Sobre mim", conteudo = "Olá!", publico = false }));
        var editado = await Ler<RespostaBloco>(await _cliente.EnviarAsync(HttpMethod.Put, $"{Blocos}/{criado.Id}", new { titulo = "Quem sou", conteudo = "Olá de novo", publico = true }));
        var removido = await _cliente.EnviarAsync(HttpMethod.Delete, $"{Blocos}/{criado.Id}");
        var outraVez = await _cliente.EnviarAsync(HttpMethod.Delete, $"{Blocos}/{criado.Id}");

        Assert.Equal("sobre-mim", criado.Slug);
        Assert.False(criado.Publico);
        // O slug não muda ao editar: as âncoras do site continuam a funcionar.
        Assert.Equal("sobre-mim", editado.Slug);
        Assert.Equal("Quem sou", editado.Titulo);
        Assert.True(editado.Publico);
        Assert.Equal(HttpStatusCode.NoContent, removido.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, outraVez.StatusCode);
    }

    [Fact]
    public async Task Os_blocos_novos_vao_para_o_fim_e_podem_ser_reordenados()
    {
        var a = await CriarBloco("A");
        var b = await CriarBloco("B");
        var c = await CriarBloco("C");

        var reordenar = await _cliente.EnviarAsync(HttpMethod.Put, $"{Blocos}/ordem", new { ids = new[] { c.Id, a.Id, b.Id } });
        var lista = await Ler<List<RespostaBloco>>(await _cliente.GetAsync(Blocos));

        Assert.Equal([0, 1, 2], new[] { a.Ordem, b.Ordem, c.Ordem });
        Assert.Equal(HttpStatusCode.NoContent, reordenar.StatusCode);
        Assert.Equal(["C", "A", "B"], lista.Select(x => x.Titulo));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task A_nova_ordem_tem_de_ter_todos_os_blocos_uma_vez(int quantos)
    {
        var a = await CriarBloco("A");
        var b = await CriarBloco("B");
        var c = await CriarBloco("C");
        int[] ids = quantos == 2 ? [a.Id, b.Id] : [a.Id, b.Id, c.Id, a.Id];

        var resposta = await _cliente.EnviarAsync(HttpMethod.Put, $"{Blocos}/ordem", new { ids });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("ids", (await Ler<ValidationProblemDetails>(resposta)).Errors.Keys);
    }

    [Fact]
    public async Task Titulos_repetidos_geram_slugs_com_sufixo()
    {
        var primeiro = await CriarBloco("Contactos");
        var segundo = await CriarBloco("Contactos");
        var projeto1 = await CriarProjeto(new { nome = "Meu Projeto" });
        var projeto2 = await CriarProjeto(new { nome = "Meu projeto!" });

        Assert.Equal("contactos", primeiro.Slug);
        Assert.Equal("contactos-2", segundo.Slug);
        Assert.Equal("meu-projeto", projeto1.Slug);
        Assert.Equal("meu-projeto-2", projeto2.Slug);
    }

    [Fact]
    public async Task Cria_um_projeto_com_o_link_do_github_normalizado()
    {
        var projeto = await CriarProjeto(new { nome = "Portfólio", descricao = "  O meu site  ", repositorioGithub = "github.com/Miguel9669/Portef-lio-Pessoal.git" });
        var lido = await Ler<RespostaProjeto>(await _cliente.GetAsync($"{Projetos}/{projeto.Id}"));

        Assert.Equal("https://github.com/Miguel9669/Portef-lio-Pessoal", lido.RepositorioGithub);
        Assert.Equal("O meu site", lido.Descricao);
        Assert.Equal("portfolio", lido.Slug);
        Assert.False(lido.Publico);
    }

    [Theory]
    [InlineData("https://gitlab.com/dono/repositorio")]
    [InlineData("https://github.com.evil.example/dono/repositorio")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    public async Task Um_link_que_nao_seja_do_github_e_recusado(string link)
    {
        var criar = await _cliente.PostAsync(Projetos, new { nome = "X", repositorioGithub = link });
        var projeto = await CriarProjeto(new { nome = "Y" });
        var editar = await _cliente.EnviarAsync(HttpMethod.Put, $"{Projetos}/{projeto.Id}", new { nome = "Y", repositorioGithub = link });

        Assert.Equal(HttpStatusCode.BadRequest, criar.StatusCode);
        Assert.Contains("RepositorioGithub", (await Ler<ValidationProblemDetails>(criar)).Errors.Keys);
        Assert.Equal(HttpStatusCode.BadRequest, editar.StatusCode);
    }

    [Fact]
    public async Task Guarda_o_link_oficial_e_a_escolha_do_codigo_privado()
    {
        var projeto = await CriarProjeto(new { nome = "Sério", linkOficial = "  https://MeuApp.pt/inicio  ", codigoPrivado = true, historicoPublico = true });
        var editado = await Ler<RespostaProjeto>(await _cliente.EnviarAsync(HttpMethod.Put, $"{Projetos}/{projeto.Id}", new { nome = "Sério" }));

        Assert.Equal("https://meuapp.pt/inicio", projeto.LinkOficial);
        Assert.True(projeto.CodigoPrivado);
        Assert.True(projeto.HistoricoPublico);
        Assert.Null(editado.LinkOficial);
        Assert.False(editado.CodigoPrivado);
    }

    [Theory]
    [InlineData("http://meuapp.pt")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://banco.pt@evil.example")]
    public async Task Um_link_oficial_que_nao_seja_https_e_recusado(string link)
    {
        var resposta = await _cliente.PostAsync(Projetos, new { nome = "X", linkOficial = link });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("LinkOficial", (await Ler<ValidationProblemDetails>(resposta)).Errors.Keys);
    }

    [Fact]
    public async Task Edita_publica_despublica_e_remove_um_projeto()
    {
        var projeto = await CriarProjeto(new { nome = "Antigo", repositorioGithub = "https://github.com/dono/repo" });

        var editado = await Ler<RespostaProjeto>(await _cliente.EnviarAsync(HttpMethod.Put, $"{Projetos}/{projeto.Id}", new { nome = "Novo nome" }));
        var publicado = await Ler<RespostaProjeto>(await _cliente.EnviarAsync(HttpMethod.Put, $"{Projetos}/{projeto.Id}/publico", new { publico = true }));
        var despublicado = await Ler<RespostaProjeto>(await _cliente.EnviarAsync(HttpMethod.Put, $"{Projetos}/{projeto.Id}/publico", new { publico = false }));
        var lista = await Ler<List<RespostaProjeto>>(await _cliente.GetAsync(Projetos));
        var removido = await _cliente.EnviarAsync(HttpMethod.Delete, $"{Projetos}/{projeto.Id}");
        var depois = await _cliente.GetAsync($"{Projetos}/{projeto.Id}");

        Assert.Equal("Novo nome", editado.Nome);
        Assert.Equal("antigo", editado.Slug);
        Assert.Null(editado.RepositorioGithub);
        Assert.True(publicado.Publico);
        // RF08: despublicado continua no backoffice.
        Assert.False(despublicado.Publico);
        Assert.Single(lista);
        Assert.Equal(HttpStatusCode.NoContent, removido.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, depois.StatusCode);
    }

    [Fact]
    public async Task Operacoes_num_projeto_inexistente_dao_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.EnviarAsync(HttpMethod.Put, $"{Projetos}/999", new { nome = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.EnviarAsync(HttpMethod.Put, $"{Projetos}/999/publico", new { publico = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.EnviarAsync(HttpMethod.Delete, $"{Projetos}/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.EnviarAsync(HttpMethod.Put, $"{Blocos}/999", new { titulo = "X", conteudo = "", publico = false })).StatusCode);
    }

    [Fact]
    public async Task Campos_obrigatorios_em_falta_dao_400()
    {
        var semNome = await _cliente.PostAsync(Projetos, new { nome = "   " });
        var semTitulo = await _cliente.PostAsync(Blocos, new { conteudo = "x", publico = true });

        Assert.Equal(HttpStatusCode.BadRequest, semNome.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, semTitulo.StatusCode);
    }

    [Fact]
    public async Task A_pre_visualizacao_devolve_o_html_sanitizado()
    {
        var resposta = await _cliente.PostAsync("/api/backoffice/markdown/pre-visualizar",
            new { texto = "**Olá** <script>alert(1)</script> [x](javascript:alert(1))" });
        var html = (await Ler<RespostaMarkdown>(resposta)).Html;

        Assert.Contains("<strong>Olá</strong>", html);
        Assert.DoesNotContain("<script", html);
        Assert.DoesNotContain("javascript:", html);
    }

    private async Task<RespostaBloco> CriarBloco(string titulo) =>
        await Ler<RespostaBloco>(await _cliente.PostAsync(Blocos, new { titulo, conteudo = $"Texto de {titulo}", publico = true }));

    private async Task<RespostaProjeto> CriarProjeto(object pedido)
    {
        var resposta = await _cliente.PostAsync(Projetos, pedido);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return await Ler<RespostaProjeto>(resposta);
    }

    private static async Task<T> Ler<T>(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<T>(Ct))!;
}
