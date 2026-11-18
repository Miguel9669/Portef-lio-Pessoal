using System.Net;
using Microsoft.EntityFrameworkCore;
using Portfolio.Api.Comandos;
using Portfolio.Tests.BaseDeDados;

namespace Portfolio.Tests.Autenticacao;

// Comandos criar-admin e repor-password (SEG08), com uma consola falsa.
[Collection("BaseDeDados")]
public sealed class ComandosContaTests(PostgresFixture bd) : IAsyncLifetime
{
    private FabricaApi _api = null!;

    public async ValueTask InitializeAsync()
    {
        bd.Exigir();
        await using (var contexto = bd.NovoBackoffice())
            await contexto.Database.ExecuteSqlRawAsync("DELETE FROM \"Utilizador\";", TestContext.Current.CancellationToken);
        _api = new FabricaApi(bd);
    }

    public async ValueTask DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task Criar_admin_cria_a_conta_uma_unica_vez()
    {
        var primeira = new ConsolaFalsa("Admin", "admin@exemplo.pt", "password-muito-boa", "password-muito-boa");
        var segunda = new ConsolaFalsa("Outro", "outro@exemplo.pt", "password-muito-boa", "password-muito-boa");

        var criada = await Executar("criar-admin", primeira);
        var recusada = await Executar("criar-admin", segunda);

        Assert.Equal(0, criada);
        Assert.Equal(1, recusada);
        Assert.Contains("Já existe uma conta", segunda.Saida);
        await using var contexto = bd.NovoBackoffice();
        Assert.Equal("admin@exemplo.pt", (await contexto.Utilizadores.SingleAsync(TestContext.Current.CancellationToken)).Email);
    }

    [Theory]
    [InlineData("curta", "curta", "entre 12 e")]
    [InlineData("password-muito-boa", "outra-password-boa", "não coincidem")]
    public async Task Criar_admin_recusa_passwords_fracas_ou_diferentes(string password, string repetida, string mensagem)
    {
        var consola = new ConsolaFalsa("Admin", "admin@exemplo.pt", password, repetida);

        var resultado = await Executar("criar-admin", consola);

        Assert.Equal(1, resultado);
        Assert.Contains(mensagem, consola.Saida);
    }

    [Fact]
    public async Task Criar_admin_recusa_um_email_invalido()
    {
        var consola = new ConsolaFalsa("Admin", "sem-arroba", "password-muito-boa", "password-muito-boa");

        Assert.Equal(1, await Executar("criar-admin", consola));
        Assert.Contains("Nome ou email inválidos", consola.Saida);
    }

    [Fact]
    public async Task Repor_password_sem_conta_avisa_que_nao_existe()
    {
        var consola = new ConsolaFalsa("s", "password-muito-boa", "password-muito-boa");

        Assert.Equal(1, await Executar("repor-password", consola));
        Assert.Contains("Ainda não existe nenhuma conta", consola.Saida);
    }

    [Fact]
    public async Task Repor_password_pode_ser_cancelado()
    {
        await _api.CriarAdminAsync();
        var consola = new ConsolaFalsa("n");

        Assert.Equal(1, await Executar("repor-password", consola));
        Assert.Contains("Cancelado", consola.Saida);
    }

    [Fact]
    public async Task Repor_password_termina_as_sessoes_e_desativa_o_2fa()
    {
        await _api.CriarAdminAsync();
        var cliente = await _api.NovoClienteComCsrfAsync();
        await _api.EntrarComDoisFatoresAsync(cliente);
        const string nova = "outra-password-comprida";

        var resultado = await Executar("repor-password", new ConsolaFalsa("s", nova, nova));
        var eu = await cliente.GetAsync("/api/backoffice/auth/eu");
        var antiga = await _api.LoginAsync(await _api.NovoClienteComCsrfAsync());
        var login = await _api.LoginAsync(await _api.NovoClienteComCsrfAsync(), password: nova);

        Assert.Equal(0, resultado);
        Assert.Equal(HttpStatusCode.Unauthorized, eu.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, antiga.StatusCode);
        Assert.Contains("configurar-2fa", await login.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        await using var contexto = bd.NovoBackoffice();
        Assert.Equal(0, await contexto.CodigosRecuperacao.CountAsync(TestContext.Current.CancellationToken));
    }

    private Task<int> Executar(string comando, ConsolaFalsa consola) =>
        ComandosConta.ExecutarAsync(comando, _api.Services, consola, TestContext.Current.CancellationToken);

    // Responde às perguntas pela ordem dada e guarda o que é escrito.
    private sealed class ConsolaFalsa(params string[] respostas) : IConsola
    {
        private readonly Queue<string> _respostas = new(respostas);
        private readonly List<string> _saida = [];

        public string Saida => string.Join('\n', _saida);

        public string LerLinha(string pergunta) => _respostas.Dequeue();

        public string LerPassword(string pergunta) => _respostas.Dequeue();

        public void Escrever(string texto) => _saida.Add(texto);
    }
}
