using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portfolio.Tests.Autenticacao;
using Portfolio.Tests.BaseDeDados;

namespace Portfolio.Tests.Seguranca;

// SEG27: os logins com e sem sucesso ficam registados (resultado, email tentado e IP; a data
// vem do próprio registo), e a password nunca aparece.
[Collection("BaseDeDados")]
public sealed class RegistoLoginsTests(PostgresFixture bd) : IAsyncLifetime
{
    private readonly Capturador _registos = new();
    private FabricaApi _api = null!;

    public async ValueTask InitializeAsync()
    {
        bd.Exigir();
        await using (var contexto = bd.NovoBackoffice())
            await contexto.Database.ExecuteSqlRawAsync("DELETE FROM \"Utilizador\";", TestContext.Current.CancellationToken);
        _api = new FabricaApi(bd, servicos: s => s.AddSingleton<ILoggerProvider>(_registos));
        await _api.CriarAdminAsync();
    }

    public async ValueTask DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task Os_logins_sao_registados_sem_a_password()
    {
        const string errada = "password-errada-secreta";
        var cliente = await _api.NovoClienteComCsrfAsync();

        await _api.LoginAsync(cliente, password: errada);
        await _api.LoginAsync(cliente, email: "intruso@exemplo.pt", password: errada);
        await _api.LoginAsync(cliente);

        var logins = _registos.Mensagens.Where(m => m.StartsWith("Login ", StringComparison.Ordinal)).ToList();
        Assert.Contains(logins, m => m.Contains("CredenciaisErradas") && m.Contains(FabricaApi.Email));
        Assert.Contains(logins, m => m.Contains("CredenciaisErradas") && m.Contains("intruso@exemplo.pt"));
        Assert.Contains(logins, m => m.Contains("Sucesso") && m.Contains(FabricaApi.Email));
        Assert.DoesNotContain(_registos.Mensagens, m => m.Contains(errada) || m.Contains(FabricaApi.Password));
    }

    // Guarda todas as mensagens de registo da API durante o teste.
    private sealed class Capturador : ILoggerProvider, ILogger
    {
        private readonly ConcurrentQueue<string> _mensagens = new();

        public IReadOnlyCollection<string> Mensagens => _mensagens;

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            _mensagens.Enqueue(formatter(state, exception));

        public void Dispose()
        {
        }
    }
}
