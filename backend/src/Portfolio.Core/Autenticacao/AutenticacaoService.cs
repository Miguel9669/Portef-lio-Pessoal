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

    // stampDispositivo: o security stamp guardado no cookie de dispositivo de confiança, se houver.
    public async Task<ResultadoLogin> EntrarAsync(string email, string password, string? stampDispositivo, CancellationToken ct)
    {
        var utilizador = await utilizadores.ObterPorEmailAsync(email, ct);
        if (utilizador is null)
        {
            hasher.Verificar(password, HashFalso());
            return new(EstadoLogin.CredenciaisErradas);
        }

        var agora = relogio.GetUtcNow().UtcDateTime;
        var passwordCerta = hasher.Verificar(password, utilizador.PasswordHash);
        var confiavel = BloqueioConta.DispositivoConfiavel(utilizador, stampDispositivo);

        // Durante o bloqueio nem a password certa entra (exceto num dispositivo de confiança),
        // e as tentativas não prolongam o bloqueio.
        if (BloqueioConta.Bloqueada(utilizador, agora) && !confiavel)
            return new(EstadoLogin.Bloqueado);

        if (!passwordCerta)
        {
            BloqueioConta.RegistarFalha(utilizador, agora, opcoes);
            await utilizadores.GravarAsync(ct);
            return new(BloqueioConta.Bloqueada(utilizador, agora) ? EstadoLogin.Bloqueado : EstadoLogin.CredenciaisErradas);
        }

        // O contador não volta a 0 aqui, só depois do código do 2FA (ver BloqueioConta).
        return new(EstadoLogin.Sucesso, utilizador);
    }

    private string HashFalso() => _hashFalso ??= hasher.Hash(Guid.NewGuid().ToString());
}
