namespace Portfolio.Core.Entidades;

// O utilizador do backoffice. Só pode existir um (RF02): a base de dados recusa um segundo.
public class Utilizador : ITemCriadoEm
{
    // Sempre 1: não é gerado pela base de dados. Um segundo utilizador é recusado
    // pela chave primária, e o CHECK (Id = 1) impede qualquer outro valor.
    public int Id { get; set; } = 1;
    public required string Nome { get; set; }
    public required string Email { get; set; }

    // Argon2id em formato PHC; o salt e os parâmetros vão dentro da string (RNF01).
    public required string PasswordHash { get; set; }

    // Muda quando a password muda; invalida todas as sessões abertas (SEG12).
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    // 2FA (SEG09). O segredo é guardado cifrado; null até o 2FA ser configurado.
    public string? TotpSegredoCifrado { get; set; }
    public bool TotpAtivo { get; set; }
    public long? TotpUltimoPasso { get; set; }

    // Bloqueio da conta (SEG06).
    public int TentativasFalhadas { get; set; }
    public DateTime? BloqueadoAte { get; set; }

    public DateTime CriadoEm { get; set; }

    public List<CodigoRecuperacao> CodigosRecuperacao { get; set; } = [];
}
