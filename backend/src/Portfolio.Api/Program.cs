using Portfolio.Api.Comandos;
using Portfolio.Api.Configuracao;
using Portfolio.Infrastructure.Dados;

var builder = WebApplication.CreateBuilder(args);

// Comando de linha: dotnet run -- migrar
// Aplica as migrações com o utilizador portfolio_migracoes (SEG01) e termina.
if (args is ["migrar", ..])
{
    await AplicadorMigracoes.AplicarAsync(builder.Configuration);
    Console.WriteLine("Migrações aplicadas.");
    return 0;
}

builder.Services.AddPortfolioApi(builder.Configuration);

var app = builder.Build();

// Comandos de linha: dotnet run -- criar-admin | repor-password (SEG08)
if (args is [var comando, ..] && ComandosConta.Reconhece(comando))
    return await ComandosConta.ExecutarAsync(comando, app.Services, new ConsolaSistema(), CancellationToken.None);

app.UsePortfolioApi();
app.Run();
return 0;
