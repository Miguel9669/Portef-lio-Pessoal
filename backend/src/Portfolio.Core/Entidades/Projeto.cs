namespace Portfolio.Core.Entidades;

// Projeto do portfólio (RF06–RF09).
public class Projeto : ITemDatas
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public required string Slug { get; set; }
    public string? Descricao { get; set; }

    // Link do repositório no GitHub, no formato https://github.com/dono/repositorio (RF10, SEG29).
    // A timeline do projeto são os commits deste repositório; null = sem timeline.
    public string? RepositorioGithub { get; set; }

    // Repositório privado: o site não mostra o link do código, e a timeline só aparece no site
    // se HistoricoPublico estiver ligado (as mensagens dos commits deixam de ser privadas). SEG29.
    public bool CodigoPrivado { get; set; }
    public bool HistoricoPublico { get; set; }

    // Site oficial do projeto, só https:// (SEG30). null = sem botão "Visitar site".
    public string? LinkOficial { get; set; }

    // O que o site pode mostrar do repositório e da timeline.
    public bool MostraCodigo => RepositorioGithub is not null && !CodigoPrivado;
    public bool MostraHistorico => RepositorioGithub is not null && (!CodigoPrivado || HistoricoPublico);

    public bool Publico { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }

    public List<ImagemProjeto> Imagens { get; set; } = [];
}
