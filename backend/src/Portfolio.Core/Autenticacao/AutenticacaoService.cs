using Portfolio.Core.Entidades;

namespace Portfolio.Core.Autenticacao;

public enum EstadoLogin { Sucesso, CredenciaisErradas, Bloqueado }

// Utilizador só preenchido com Sucesso. Bloqueado serve só para o registo (SEG27):
// para quem faz o pedido, a resposta é igual à de credenciais erradas (SEG04).
public sealed record ResultadoLogin(EstadoLogin Estado, Utilizador? Utilizador = null);

// Primeiro passo do login: email e password (RF01, SEG04–SEG06).
public sealed class AutenticacaoService(
    IRepositorioUtilizador utilizadores,
    IPasswordHasher hasher,
    TimeProvider relogio,
    OpcoesAutenticacao opcoes)
{
    // Hash de uma password aleatória, para gastar o mesmo tempo quando o email não existe (SEG05).
    // Calculado uma só vez: se fosse por pedido, esse caminho demorava o dobro.
    private static string? _hashFalso;

    public async Task<ResultadoLogin> EntrarAsync(string email, string password, CancellationToken ct)
    {
        var utilizador = await utilizadores.ObterPorEmailAsync(email, ct);
        if (utilizador is null)
        {
            hasher.Verificar(password, HashFalso());
            return new(EstadoLogin.CredenciaisErradas);
        }

        var agora = relogio.GetUtcNow().UtcDateTime;
        var passwordCerta = hasher.Verificar(password, utilizador.PasswordHash);

        // Durante o bloqueio nem a password certa entra, e as tentativas não prolongam o bloqueio.
        if (utilizador.BloqueadoAte > agora)
            return new(EstadoLogin.Bloqueado);

        if (!passwordCerta)
        {
            RegistarFalha(utilizador, agora);
            await utilizadores.GravarAsync(ct);
            return new(utilizador.BloqueadoAte > agora ? EstadoLogin.Bloqueado : EstadoLogin.CredenciaisErradas);
        }

        utilizador.TentativasFalhadas = 0;
        utilizador.BloqueadoAte = null;
        await utilizadores.GravarAsync(ct);
        return new(EstadoLogin.Sucesso, utilizador);
    }

    // Bloqueio progressivo (SEG06): à 5.ª falha seguida 1 min, à 6.ª 5 min, a partir da 7.ª 15 min.
    private void RegistarFalha(Utilizador utilizador, DateTime agora)
    {
        utilizador.TentativasFalhadas++;
        var excesso = utilizador.TentativasFalhadas - opcoes.FalhasAntesDoBloqueio;
        if (excesso < 0)
            return;

        var duracoes = opcoes.Duracoes;
        var duracao = duracoes[Math.Min(excesso, duracoes.Count - 1)];
        utilizador.BloqueadoAte = agora + duracao;
    }

    private string HashFalso() => _hashFalso ??= hasher.Hash(Guid.NewGuid().ToString());
}
