namespace Portfolio.Core.Entidades;

// Secção do site gerida no backoffice (RF04, RF05). Conteúdo em Markdown.
public class BlocoConteudo : ITemDatas
{
    public int Id { get; set; }
    public required string Titulo { get; set; }
    public required string Slug { get; set; }
    public string Conteudo { get; set; } = "";
    public int Ordem { get; set; }
    public bool Publico { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
