using Portfolio.Core.Entidades;

namespace Portfolio.Core.Autenticacao;

// Bloqueio progressivo da conta (SEG06), partilhado pela password e pelo código do 2FA:
// à 5.ª falha seguida 1 min, à 6.ª 5 min, a partir da 7.ª 15 min.
// O contador só volta a 0 depois de um login completo (password e código). Se uma password certa
// o repusesse, quem soubesse a password podia tentar códigos sem nunca chegar ao bloqueio.
public static class BloqueioConta
{
    public static bool Bloqueada(Utilizador utilizador, DateTime agora) => utilizador.BloqueadoAte > agora;

    // Um dispositivo de confiança é um browser onde já se fez um login completo com o security
    // stamp atual (o repor-password muda-o e invalida todos). Nele, o bloqueio não se aplica:
    // assim um atacante que falhe de propósito não consegue manter o dono fora da conta.
    public static bool DispositivoConfiavel(Utilizador utilizador, string? stampDispositivo) =>
        stampDispositivo is not null && stampDispositivo == utilizador.SecurityStamp;

    public static void RegistarFalha(Utilizador utilizador, DateTime agora, OpcoesAutenticacao opcoes)
    {
        utilizador.TentativasFalhadas++;
        var excesso = utilizador.TentativasFalhadas - opcoes.FalhasAntesDoBloqueio;
        if (excesso < 0)
            return;

        var duracoes = opcoes.Duracoes;
        utilizador.BloqueadoAte = agora + duracoes[Math.Min(excesso, duracoes.Count - 1)];
    }

    public static void Limpar(Utilizador utilizador)
    {
        utilizador.TentativasFalhadas = 0;
        utilizador.BloqueadoAte = null;
    }
}
