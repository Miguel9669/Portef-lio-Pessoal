using Portfolio.Core.Entidades;

namespace Portfolio.Core.Autenticacao;

// Hash das passwords (RNF01). Implementado com Argon2id na Infrastructure.
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verificar(string password, string hash);
}

// TOTP do 2FA (SEG09): gera o segredo e o QR code e valida os códigos.
public interface ITotp
{
    string GerarSegredo();
    ConfiguracaoTotp Configuracao(string segredo, string conta);

    // Devolve o intervalo de 30 s a que o código corresponde, ou null se for inválido.
    long? Validar(string segredo, string codigo, DateTimeOffset agora);
}

// Segredo em base32 (para escrever à mão), URI otpauth:// e QR code em PNG (data URI).
public record ConfiguracaoTotp(string Segredo, string Uri, string QrCode);

// Cifra do segredo TOTP (SEG09): tem de ser lido para validar os códigos, por isso não pode ser um hash.
public interface ICifraSegredos
{
    string Cifrar(string texto);
    string Decifrar(string cifrado);
}

// Acesso ao único utilizador (RF02).
public interface IRepositorioUtilizador
{
    Task<Utilizador?> ObterPorEmailAsync(string email, CancellationToken ct);

    // Com os códigos de recuperação.
    Task<Utilizador?> ObterAsync(CancellationToken ct);

    Task<bool> ExisteAsync(CancellationToken ct);
    Task<string?> ObterSecurityStampAsync(CancellationToken ct);
    void Adicionar(Utilizador utilizador);
    Task GravarAsync(CancellationToken ct);
}
