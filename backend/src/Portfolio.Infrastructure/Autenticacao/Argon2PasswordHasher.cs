using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Portfolio.Core.Autenticacao;

namespace Portfolio.Infrastructure.Autenticacao;

// Argon2id em formato PHC (RNF01, SEG07): $argon2id$v=19$m=19456,t=2,p=1$<salt>$<hash>
// O salt e os parâmetros vão dentro da string, por isso podem mudar no futuro sem
// invalidar as passwords antigas.
public sealed class Argon2PasswordHasher : IPasswordHasher
{
    // Parâmetros mínimos recomendados pela OWASP: 19 MiB de memória, 2 iterações, 1 thread.
    private const int Memoria = 19456;
    private const int Iteracoes = 2;
    private const int Paralelismo = 1;
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;

    // Cada cálculo usa 19 MiB de memória. Muitos logins ao mesmo tempo (de muitos IPs, que o rate
    // limit por IP não trava) podiam esgotar a memória do servidor; assim ficam em fila.
    private const int MaximoEmSimultaneo = 4;
    private static readonly SemaphoreSlim EmSimultaneo = new(MaximoEmSimultaneo, MaximoEmSimultaneo);

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Calcular(password, new Parametros(Memoria, Iteracoes, Paralelismo, salt, new byte[TamanhoHash]));
        return $"$argon2id$v=19$m={Memoria},t={Iteracoes},p={Paralelismo}${Base64(salt)}${Base64(hash)}";
    }

    public bool Verificar(string password, string hash)
    {
        if (!Parametros.TentarLer(hash, out var p))
            return false;
        return CryptographicOperations.FixedTimeEquals(Calcular(password, p), p.Hash);
    }

    private static byte[] Calcular(string password, Parametros p)
    {
        EmSimultaneo.Wait();
        try
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = p.Salt,
                MemorySize = p.Memoria,
                Iterations = p.Iteracoes,
                DegreeOfParallelism = p.Paralelismo,
            };
            return argon2.GetBytes(p.Hash.Length);
        }
        finally
        {
            EmSimultaneo.Release();
        }
    }

    // O formato PHC usa base64 sem o "=" no fim.
    private static string Base64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=');

    private sealed record Parametros(int Memoria, int Iteracoes, int Paralelismo, byte[] Salt, byte[] Hash)
    {
        public static bool TentarLer(string phc, out Parametros parametros)
        {
            parametros = null!;
            var partes = phc.Split('$');
            if (partes is not ["", "argon2id", "v=19", var custos, var salt, var hash])
                return false;

            var valores = custos.Split(',').Select(c => c.Split('=')).ToDictionary(c => c[0], c => c.ElementAtOrDefault(1));
            if (!Inteiro(valores, "m", out var m) || !Inteiro(valores, "t", out var t) || !Inteiro(valores, "p", out var par))
                return false;

            try
            {
                parametros = new Parametros(m, t, par, DeBase64(salt), DeBase64(hash));
                return parametros.Hash.Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool Inteiro(Dictionary<string, string?> valores, string chave, out int valor)
        {
            valor = 0;
            return valores.TryGetValue(chave, out var texto)
                && int.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out valor)
                && valor > 0;
        }

        private static byte[] DeBase64(string texto) =>
            Convert.FromBase64String(texto.PadRight(texto.Length + ((4 - (texto.Length % 4)) % 4), '='));
    }
}
