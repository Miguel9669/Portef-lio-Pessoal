using Portfolio.Api.Comandos;
using Portfolio.Api.Configuracao;
using Portfolio.Api.Projetos;
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

builder.WebHost.ConfigureKestrel(o =>
{
    // SEG18: limite global do corpo dos pedidos (o maior é o envio de uma imagem).
    o.Limits.MaxRequestBodySize = ImagensController.TamanhoMaximoPedido;
    // Sem o cabeçalho "Server: Kestrel", que só serve para dizer a um atacante o que procurar.
    o.AddServerHeader = false;
});

var app = builder.Build();

// Comandos de linha: dotnet run -- criar-admin | repor-password (SEG08)
if (args is [var comando, ..] && ComandosConta.Reconhece(comando))
    return await ComandosConta.ExecutarAsync(comando, app.Services, new ConsolaSistema(), CancellationToken.None);

app.UsePortfolioApi();
app.Run();
return 0;
