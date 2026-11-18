namespace Portfolio.Core.Autenticacao;

// Valores da secção "Autenticacao" do appsettings.json (ver docs/03-seguranca.md, "Valores por defeito").
public sealed class OpcoesAutenticacao
{
    // SEG07
    public int TamanhoMinimoPassword { get; set; } = 12;

    // SEG06: depois de N falhas seguidas, a conta fica bloqueada por 1 min, depois 5 min, depois 15 min.
    public int FalhasAntesDoBloqueio { get; set; } = 5;

    // Vazio por defeito: o binder da configuração acrescenta aos valores iniciais de um array
    // em vez de os substituir. Sem valores configurados usa-se DuracoesBloqueioPredefinidas.
    public TimeSpan[] DuracoesBloqueio { get; set; } = [];

    public static readonly TimeSpan[] DuracoesBloqueioPredefinidas =
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15)];

    public IReadOnlyList<TimeSpan> Duracoes =>
        DuracoesBloqueio.Length > 0 ? DuracoesBloqueio : DuracoesBloqueioPredefinidas;
}
