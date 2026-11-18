namespace Portfolio.Core.Entidades;

// Campos comuns às imagens. O ficheiro fica fora da base de dados (RNF05).
public abstract class Imagem : ITemCriadoEm
{
    public int Id { get; set; }

    // Nome gerado (GUID + extensão); nunca o nome enviado.
    public required string Ficheiro { get; set; }

    // Só para mostrar; nunca usado como caminho.
    public required string NomeOriginal { get; set; }

    public required string TipoMime { get; set; }
    public long TamanhoBytes { get; set; }
    public int Ordem { get; set; }
    public DateTime CriadoEm { get; set; }
}

public class ImagemProjeto : Imagem
{
    public int ProjetoId { get; set; }
}

public class ImagemNota : Imagem
{
    public int NotaId { get; set; }
}
