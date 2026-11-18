namespace Portfolio.Core.Entidades;

// Projeto do portfólio (RF06–RF09).
public class Projeto : ITemDatas
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public required string Slug { get; set; }
    public string? Descricao { get; set; }
    public bool Publico { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }

    public List<Nota> Notas { get; set; } = [];
    public List<ImagemProjeto> Imagens { get; set; } = [];
}
