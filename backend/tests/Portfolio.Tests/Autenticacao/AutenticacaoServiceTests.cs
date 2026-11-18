using Microsoft.Extensions.Time.Testing;
using Portfolio.Core.Autenticacao;
using Portfolio.Core.Entidades;

namespace Portfolio.Tests.Autenticacao;

// Regras do login sem base de dados nem HTTP (SEG05, SEG06).
public class AutenticacaoServiceTests
{
    private readonly FakeTimeProvider _relogio = new(DateTimeOffset.UtcNow);
    private readonly RepositorioFalso _repositorio = new();
    private readonly HasherFalso _hasher = new();
    private readonly AutenticacaoService _servico;

    public AutenticacaoServiceTests()
    {
        _repositorio.Utilizador = new Utilizador { Nome = "Admin", Email = "admin@exemplo.pt", PasswordHash = "hash:certa" };
        _servico = new AutenticacaoService(_repositorio, _hasher, _relogio, new OpcoesAutenticacao());
    }

    [Fact]
    public async Task O_bloqueio_e_progressivo_1_5_e_15_minutos()
    {
        var bloqueios = new List<TimeSpan>();
        for (var i = 0; i < 8; i++)
        {
            await Entrar("errada");
            if (_repositorio.Utilizador!.BloqueadoAte is { } ate)
            {
                bloqueios.Add(ate - _relogio.GetUtcNow().UtcDateTime);
                _relogio.SetUtcNow(ate.AddSeconds(1));
                _repositorio.Utilizador.BloqueadoAte = null;
            }
        }

        Assert.Equal([TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15)], bloqueios);
    }

    [Fact]
    public async Task Durante_o_bloqueio_as_tentativas_nao_contam_e_um_login_certo_depois_limpa_o_contador()
    {
        for (var i = 0; i < 5; i++)
            await Entrar("errada");
        var bloqueadoAte = _repositorio.Utilizador!.BloqueadoAte;

        var duranteBloqueio = await Entrar("certa");
        Assert.Equal(EstadoLogin.Bloqueado, duranteBloqueio.Estado);
        Assert.Equal(bloqueadoAte, _repositorio.Utilizador.BloqueadoAte);
        Assert.Equal(5, _repositorio.Utilizador.TentativasFalhadas);

        _relogio.Advance(TimeSpan.FromMinutes(2));
        var depois = await Entrar("certa");

        Assert.Equal(EstadoLogin.Sucesso, depois.Estado);
        Assert.Equal(0, _repositorio.Utilizador.TentativasFalhadas);
        Assert.Null(_repositorio.Utilizador.BloqueadoAte);
    }

    [Fact]
    public async Task Com_um_email_inexistente_verifica_na_mesma_uma_password()
    {
        _repositorio.Utilizador = null;

        var resultado = await Entrar("certa");

        Assert.Equal(EstadoLogin.CredenciaisErradas, resultado.Estado);
        Assert.Equal(1, _hasher.Verificacoes);
    }

    private Task<ResultadoLogin> Entrar(string password) =>
        _servico.EntrarAsync("admin@exemplo.pt", password, TestContext.Current.CancellationToken);

    private sealed class HasherFalso : IPasswordHasher
    {
        public int Verificacoes { get; private set; }

        public string Hash(string password) => "hash:" + password;

        public bool Verificar(string password, string hash)
        {
            Verificacoes++;
            return hash == "hash:" + password;
        }
    }

    private sealed class RepositorioFalso : IRepositorioUtilizador
    {
        public Utilizador? Utilizador { get; set; }

        public Task<Utilizador?> ObterPorEmailAsync(string email, CancellationToken ct) => Task.FromResult(Utilizador);

        public Task<Utilizador?> ObterAsync(CancellationToken ct) => Task.FromResult(Utilizador);

        public Task<bool> ExisteAsync(CancellationToken ct) => Task.FromResult(Utilizador is not null);

        public Task<string?> ObterSecurityStampAsync(CancellationToken ct) => Task.FromResult(Utilizador?.SecurityStamp);

        public void Adicionar(Utilizador utilizador) => Utilizador = utilizador;

        public Task GravarAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
