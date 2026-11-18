using Portfolio.Infrastructure;
using Portfolio.Infrastructure.Dados;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Comando de linha: dotnet run -- migrar
// Aplica as migrações com o utilizador portfolio_migracoes (SEG01) e termina.
if (args is ["migrar", ..])
{
    await AplicadorMigracoes.AplicarAsync(builder.Configuration);
    Console.WriteLine("Migrações aplicadas.");
    return;
}

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Documentação da API, só em desenvolvimento:
    // /openapi/v1.json (especificação) e /scalar (página para testar os endpoints).
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
