using System.Diagnostics.CodeAnalysis;
using System.Text;
using Portfolio.Core.Autenticacao;

namespace Portfolio.Api.Comandos;

// Entrada e saída dos comandos, separada para os testes.
public interface IConsola
{
    string LerLinha(string pergunta);

    // Sem mostrar o que se escreve. A password nunca é passada como argumento,
    // porque ficaria no histórico da shell (SEG08).
    string LerPassword(string pergunta);

    void Escrever(string texto);
}

// Gestão da conta por linha de comandos, só para quem tem acesso ao servidor (SEG08):
//   dotnet run -- criar-admin
//   dotnet run -- repor-password
public static class ComandosConta
{
    public static bool Reconhece(string comando) => comando is "criar-admin" or "repor-password";

    public static async Task<int> ExecutarAsync(string comando, IServiceProvider servicos, IConsola consola, CancellationToken ct)
    {
        await using var ambito = servicos.CreateAsyncScope();
        var contas = ambito.ServiceProvider.GetRequiredService<ContaService>();
        var opcoes = ambito.ServiceProvider.GetRequiredService<OpcoesAutenticacao>();

        return comando == "criar-admin"
            ? await CriarAdminAsync(contas, opcoes, consola, ct)
            : await ReporPasswordAsync(contas, opcoes, consola, ct);
    }

    private static async Task<int> CriarAdminAsync(ContaService contas, OpcoesAutenticacao opcoes, IConsola consola, CancellationToken ct)
    {
        var nome = consola.LerLinha("Nome: ");
        var email = consola.LerLinha("Email: ");
        var password = LerPasswordConfirmada(consola, opcoes);
        if (password is null)
            return 1;

        var resultado = await contas.CriarAdminAsync(nome, email, password, ct);
        return Terminar(consola, resultado, opcoes, "Conta criada. No primeiro login vai ser pedido para configurar o 2FA.");
    }

    private static async Task<int> ReporPasswordAsync(ContaService contas, OpcoesAutenticacao opcoes, IConsola consola, CancellationToken ct)
    {
        consola.Escrever("Isto define uma password nova, desativa o 2FA, apaga os códigos de recuperação e termina todas as sessões.");
        if (!consola.LerLinha("Continuar? (s/N) ").Trim().Equals("s", StringComparison.OrdinalIgnoreCase))
        {
            consola.Escrever("Cancelado.");
            return 1;
        }

        var password = LerPasswordConfirmada(consola, opcoes);
        if (password is null)
            return 1;

        var resultado = await contas.ReporPasswordAsync(password, ct);
        return Terminar(consola, resultado, opcoes, "Password reposta. No próximo login vai ser pedido para configurar o 2FA de novo.");
    }

    private static string? LerPasswordConfirmada(IConsola consola, OpcoesAutenticacao opcoes)
    {
        var password = consola.LerPassword($"Password (mínimo {opcoes.TamanhoMinimoPassword} caracteres): ");
        if (password == consola.LerPassword("Repetir a password: "))
            return password;

        consola.Escrever("As passwords não coincidem.");
        return null;
    }

    private static int Terminar(IConsola consola, ResultadoConta resultado, OpcoesAutenticacao opcoes, string sucesso)
    {
        consola.Escrever(resultado switch
        {
            ResultadoConta.Sucesso => sucesso,
            ResultadoConta.JaExiste => "Já existe uma conta. Para recuperar o acesso: repor-password.",
            ResultadoConta.SemConta => "Ainda não existe nenhuma conta. Para a criar: criar-admin.",
            ResultadoConta.PasswordFraca => $"A password tem de ter entre {opcoes.TamanhoMinimoPassword} e {ContaService.TamanhoMaximoPassword} caracteres.",
            _ => "Nome ou email inválidos.",
        });
        return resultado == ResultadoConta.Sucesso ? 0 : 1;
    }
}

// Lê do terminal. Não tem testes automáticos: depende de um terminal interativo.
[ExcludeFromCodeCoverage(Justification = "Entrada interativa do terminal; a lógica está em ComandosConta.")]
public sealed class ConsolaSistema : IConsola
{
    public string LerLinha(string pergunta)
    {
        Console.Write(pergunta);
        return Console.ReadLine() ?? "";
    }

    public string LerPassword(string pergunta)
    {
        Console.Write(pergunta);
        if (Console.IsInputRedirected)
            return Console.ReadLine() ?? "";

        var password = new StringBuilder();
        for (var tecla = Console.ReadKey(intercept: true); tecla.Key != ConsoleKey.Enter; tecla = Console.ReadKey(intercept: true))
        {
            if (tecla.Key == ConsoleKey.Backspace && password.Length > 0)
                password.Length--;
            else if (!char.IsControl(tecla.KeyChar))
                password.Append(tecla.KeyChar);
        }
        Console.WriteLine();
        return password.ToString();
    }

    public void Escrever(string texto) => Console.WriteLine(texto);
}
