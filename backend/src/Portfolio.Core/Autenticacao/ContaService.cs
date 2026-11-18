using Portfolio.Core.Entidades;

namespace Portfolio.Core.Autenticacao;

public enum ResultadoConta { Sucesso, JaExiste, SemConta, PasswordFraca, DadosInvalidos }

// Gestão da conta, usada só pelos comandos criar-admin e repor-password (SEG08).
public sealed class ContaService(IRepositorioUtilizador utilizadores, IPasswordHasher hasher, OpcoesAutenticacao opcoes)
{
    public const int TamanhoMaximoPassword = 256;

    public async Task<ResultadoConta> CriarAdminAsync(string nome, string email, string password, CancellationToken ct)
    {
        nome = nome.Trim();
        email = email.Trim();
        if (nome.Length is 0 or > 150 || email.Length > 254 || !email.Contains('@'))
            return ResultadoConta.DadosInvalidos;
        if (!PasswordValida(password))
            return ResultadoConta.PasswordFraca;

        // A base de dados também recusa uma segunda conta (CHECK Id = 1).
        if (await utilizadores.ExisteAsync(ct))
            return ResultadoConta.JaExiste;

        utilizadores.Adicionar(new Utilizador { Nome = nome, Email = email, PasswordHash = hasher.Hash(password) });
        await utilizadores.GravarAsync(ct);
        return ResultadoConta.Sucesso;
    }

    // Password nova, 2FA desativado (configura-se de novo no login seguinte), códigos de
    // recuperação apagados, todas as sessões terminadas e bloqueio limpo.
    public async Task<ResultadoConta> ReporPasswordAsync(string password, CancellationToken ct)
    {
        if (!PasswordValida(password))
            return ResultadoConta.PasswordFraca;

        var utilizador = await utilizadores.ObterAsync(ct);
        if (utilizador is null)
            return ResultadoConta.SemConta;

        utilizador.PasswordHash = hasher.Hash(password);
        utilizador.SecurityStamp = Guid.NewGuid().ToString("N");
        utilizador.TotpAtivo = false;
        utilizador.TotpSegredoCifrado = null;
        utilizador.TotpUltimoPasso = null;
        utilizador.CodigosRecuperacao.Clear();
        utilizador.TentativasFalhadas = 0;
        utilizador.BloqueadoAte = null;
        await utilizadores.GravarAsync(ct);
        return ResultadoConta.Sucesso;
    }

    private bool PasswordValida(string password) =>
        password.Length >= opcoes.TamanhoMinimoPassword && password.Length <= TamanhoMaximoPassword;
}
