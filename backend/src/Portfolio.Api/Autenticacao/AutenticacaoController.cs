using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Core.Autenticacao;

namespace Portfolio.Api.Autenticacao;

public sealed record PedidoLogin(
    [Required, MaxLength(254)] string Email,
    [Required, MaxLength(ContaService.TamanhoMaximoPassword)] string Password);

// Código da app (6 dígitos) ou de recuperação (XXXX-XXXX-XXXX-XXXX).
public sealed record PedidoCodigo([Required, MaxLength(32)] string Codigo);

// "codigo": pedir o código do 2FA. "configurar-2fa": mostrar o QR code (primeiro login).
public sealed record RespostaLogin(string ProximoPasso);

public sealed record RespostaAtivacao(IReadOnlyList<string> CodigosRecuperacao);

public sealed record RespostaUtilizador(string Nome, string Email);

// Login em dois passos: password (sessão parcial) e depois o código do 2FA (sessão completa).
// Ver o fluxo em docs/04-componentes.md.
[ApiController]
[Route("api/backoffice/auth")]
public sealed class AutenticacaoController(
    AutenticacaoService autenticacao,
    DoisFatoresService doisFatores,
    GestorSessao sessoes,
    IRepositorioUtilizador utilizadores,
    ILogger<AutenticacaoController> registo) : ControllerBase
{
    private const string CredenciaisErradas = "Email ou password incorretos";
    private const string CodigoErrado = "Código inválido";
    private const string RecomecarLogin = "Demasiadas tentativas. Volte a introduzir a password.";

    // O frontend chama este endpoint ao arrancar para receber o token CSRF (SEG11).
    [HttpGet("csrf")]
    [AllowAnonymous]
    public NoContentResult Csrf()
    {
        sessoes.EmitirTokenCsrf(User);
        return NoContent();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(Sessao.LimiteLogin)]
    public async Task<IActionResult> Login(PedidoLogin pedido, CancellationToken ct)
    {
        var resultado = await autenticacao.EntrarAsync(pedido.Email, pedido.Password, ct);

        // SEG27: data (no registo), IP e email tentado; nunca a password.
        registo.LogInformation("Login {Resultado} para {Email} a partir de {Ip}",
            resultado.Estado, pedido.Email, HttpContext.Connection.RemoteIpAddress);

        // SEG04: a mesma resposta para email inexistente, password errada e conta bloqueada.
        if (resultado.Utilizador is null)
            return Problem(title: CredenciaisErradas, statusCode: StatusCodes.Status401Unauthorized);

        await sessoes.AbrirParcialAsync(resultado.Utilizador);
        return Ok(new RespostaLogin(resultado.Utilizador.TotpAtivo ? "codigo" : "configurar-2fa"));
    }

    // Primeiro login: gera o segredo e o QR code. Pode ser chamado de novo até o 2FA ficar ativo.
    [HttpPost("2fa/configurar")]
    [Authorize(Policy = Sessao.PoliticaParcial)]
    public async Task<ActionResult<ConfiguracaoTotp>> Configurar(CancellationToken ct)
    {
        var configuracao = await doisFatores.ConfigurarAsync(ct);
        return configuracao is null
            ? Problem(title: "O 2FA já está ativo", statusCode: StatusCodes.Status409Conflict)
            : configuracao;
    }

    // Confirma o primeiro código, ativa o 2FA, abre a sessão completa e devolve os códigos de
    // recuperação. É a única vez que os códigos são mostrados.
    [HttpPost("2fa/ativar")]
    [Authorize(Policy = Sessao.PoliticaParcial)]
    [EnableRateLimiting(Sessao.LimiteCodigo)]
    public async Task<IActionResult> Ativar(PedidoCodigo pedido, CancellationToken ct)
    {
        var resultado = await doisFatores.AtivarAsync(pedido.Codigo, ct);
        switch (resultado.Estado)
        {
            case EstadoAtivacao.Ativado:
                registo.LogInformation("2FA ativado a partir de {Ip}", HttpContext.Connection.RemoteIpAddress);
                await AbrirSessaoCompletaAsync(ct);
                return Ok(new RespostaAtivacao(resultado.CodigosRecuperacao));
            case EstadoAtivacao.CodigoInvalido:
                return await CodigoInvalidoAsync();
            default:
                return Problem(title: "O 2FA não está à espera de ativação", statusCode: StatusCodes.Status409Conflict);
        }
    }

    // Segundo passo do login (SEG09).
    [HttpPost("2fa")]
    [Authorize(Policy = Sessao.PoliticaParcial)]
    [EnableRateLimiting(Sessao.LimiteCodigo)]
    public async Task<IActionResult> Codigo(PedidoCodigo pedido, CancellationToken ct)
    {
        var estado = await doisFatores.ValidarAsync(pedido.Codigo, ct);
        registo.LogInformation("Código 2FA {Resultado} a partir de {Ip}", estado, HttpContext.Connection.RemoteIpAddress);

        return estado switch
        {
            EstadoCodigo.Valido => Ok(await AbrirSessaoCompletaAsync(ct)),
            EstadoCodigo.Invalido => await CodigoInvalidoAsync(),
            _ => Problem(title: "Configure o 2FA primeiro", statusCode: StatusCodes.Status409Conflict),
        };
    }

    // Aceita qualquer estado (com sessão completa, parcial ou sem sessão).
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<NoContentResult> Logout()
    {
        await sessoes.FecharAsync();
        return NoContent();
    }

    // Para o frontend saber se tem sessão (RF03).
    [HttpGet("eu")]
    public async Task<ActionResult<RespostaUtilizador>> Eu(CancellationToken ct)
    {
        var utilizador = await utilizadores.ObterAsync(ct);
        return utilizador is null ? Unauthorized() : new RespostaUtilizador(utilizador.Nome, utilizador.Email);
    }

    private async Task<RespostaUtilizador> AbrirSessaoCompletaAsync(CancellationToken ct)
    {
        var utilizador = await utilizadores.ObterAsync(ct)
            ?? throw new InvalidOperationException("O utilizador desapareceu durante o login.");
        await sessoes.AbrirCompletaAsync(utilizador);
        return new RespostaUtilizador(utilizador.Nome, utilizador.Email);
    }

    private async Task<ObjectResult> CodigoInvalidoAsync()
    {
        var recomecar = await sessoes.RegistarCodigoErradoAsync();
        return Problem(title: recomecar ? RecomecarLogin : CodigoErrado, statusCode: StatusCodes.Status401Unauthorized);
    }
}
