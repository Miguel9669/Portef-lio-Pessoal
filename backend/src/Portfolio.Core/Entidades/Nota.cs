namespace Portfolio.Core.Entidades;

// Entrada da timeline de um projeto (RF10–RF12).
public class Nota : ITemDatas
{
    public int Id { get; set; }
    public int ProjetoId { get; set; }
    public required string Titulo { get; set; }

    // Texto em Markdown. Em C# não pode chamar-se "Nota" (nome da classe);
    // na base de dados a coluna chama-se Nota.
    public required string Texto { get; set; }

    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }

    public List<ImagemNota> Imagens { get; set; } = [];
}
