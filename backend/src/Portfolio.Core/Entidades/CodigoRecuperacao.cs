namespace Portfolio.Core.Entidades;

// Código de recuperação do 2FA, de uso único (SEG09). Só o hash é guardado.
public class CodigoRecuperacao
{
    public int Id { get; set; }
    public int UtilizadorId { get; set; }
    public required string CodigoHash { get; set; }
    public DateTime? UsadoEm { get; set; }
}
