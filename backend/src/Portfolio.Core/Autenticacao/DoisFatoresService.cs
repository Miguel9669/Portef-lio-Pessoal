using System.Security.Cryptography;
using System.Text;
using Portfolio.Core.Entidades;

namespace Portfolio.Core.Autenticacao;

public enum EstadoAtivacao { Ativado, JaAtivo, NaoConfigurado, CodigoInvalido }

public sealed record ResultadoAtivacao(EstadoAtivacao Estado, IReadOnlyList<string> CodigosRecuperacao);

public enum EstadoCodigo { Valido, Invalido, NaoAtivo }

// Segundo passo do login: TOTP e códigos de recuperação (SEG09).
public sealed class DoisFatoresService(
    IRepositorioUtilizador utilizadores,
    ITotp totp,
    ICifraSegredos cifra,
    TimeProvider relogio)
{
    public const int NumeroCodigosRecuperacao = 10;

    // Sem 0/O, 1/I/L: os códigos são escritos à mão.
    private const string AlfabetoCodigos = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int TamanhoCodigo = 16;

    // Gera um segredo novo e guarda-o cifrado. Só enquanto o 2FA não estiver ativo:
    // depois disso, mudar o segredo só com o comando repor-password (SEG08).
    public async Task<ConfiguracaoTotp?> ConfigurarAsync(CancellationToken ct)
    {
        var utilizador = await utilizadores.ObterAsync(ct);
        if (utilizador is null || utilizador.TotpAtivo)
            return null;

        var segredo = totp.GerarSegredo();
        utilizador.TotpSegredoCifrado = cifra.Cifrar(segredo);
        utilizador.TotpUltimoPasso = null;
        await utilizadores.GravarAsync(ct);
        return totp.Configuracao(segredo, utilizador.Email);
    }

    // Confirma o primeiro código, para ter a certeza de que a app ficou bem configurada,
    // e gera os códigos de recuperação. Os códigos em claro só saem daqui, uma vez.
    public async Task<ResultadoAtivacao> AtivarAsync(string codigo, CancellationToken ct)
    {
        var utilizador = await utilizadores.ObterAsync(ct);
        if (utilizador is null || utilizador.TotpSegredoCifrado is null)
            return new(EstadoAtivacao.NaoConfigurado, []);
        if (utilizador.TotpAtivo)
            return new(EstadoAtivacao.JaAtivo, []);
        if (!ValidarTotp(utilizador, codigo))
            return new(EstadoAtivacao.CodigoInvalido, []);

        utilizador.TotpAtivo = true;
        var codigos = SubstituirCodigosRecuperacao(utilizador);
        await utilizadores.GravarAsync(ct);
        return new(EstadoAtivacao.Ativado, codigos);
    }

    // Aceita o código da app ou um código de recuperação, cada um uma só vez.
    public async Task<EstadoCodigo> ValidarAsync(string codigo, CancellationToken ct)
    {
        var utilizador = await utilizadores.ObterAsync(ct);
        if (utilizador is null || !utilizador.TotpAtivo)
            return EstadoCodigo.NaoAtivo;

        var valido = ValidarTotp(utilizador, codigo) || UsarCodigoRecuperacao(utilizador, codigo);
        if (valido)
            await utilizadores.GravarAsync(ct);
        return valido ? EstadoCodigo.Valido : EstadoCodigo.Invalido;
    }

    // Um código intercetado não pode ser reutilizado nos 30 s em que ainda é válido:
    // só se aceitam intervalos posteriores ao último usado.
    private bool ValidarTotp(Utilizador utilizador, string codigo)
    {
        var segredo = cifra.Decifrar(utilizador.TotpSegredoCifrado!);
        var passo = totp.Validar(segredo, codigo, relogio.GetUtcNow());
        if (passo is null || passo <= utilizador.TotpUltimoPasso)
            return false;

        utilizador.TotpUltimoPasso = passo;
        return true;
    }

    private bool UsarCodigoRecuperacao(Utilizador utilizador, string codigo)
    {
        var hash = HashCodigo(codigo);
        var disponivel = utilizador.CodigosRecuperacao
            .FirstOrDefault(c => c.UsadoEm is null && c.CodigoHash == hash);
        if (disponivel is null)
            return false;

        disponivel.UsadoEm = relogio.GetUtcNow().UtcDateTime;
        return true;
    }

    // Gerar novos códigos apaga os antigos.
    private static List<string> SubstituirCodigosRecuperacao(Utilizador utilizador)
    {
        var codigos = Enumerable.Range(0, NumeroCodigosRecuperacao).Select(_ => NovoCodigo()).ToList();
        utilizador.CodigosRecuperacao.Clear();
        utilizador.CodigosRecuperacao.AddRange(codigos.Select(c => new CodigoRecuperacao { CodigoHash = HashCodigo(c) }));
        return codigos;
    }

    // Formato XXXX-XXXX-XXXX-XXXX (cerca de 80 bits aleatórios).
    private static string NovoCodigo()
    {
        var c = RandomNumberGenerator.GetString(AlfabetoCodigos, TamanhoCodigo);
        return $"{c[..4]}-{c[4..8]}-{c[8..12]}-{c[12..]}";
    }

    // Com 80 bits aleatórios não é preciso um hash lento como o das passwords:
    // não há dicionário que ajude a adivinhá-los. Ignora hífenes, espaços e maiúsculas.
    private static string HashCodigo(string codigo)
    {
        var normalizado = new string(codigo.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalizado)));
    }
}
