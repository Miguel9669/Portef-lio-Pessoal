using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Portfolio.Api.Autenticacao;

// As sessões ficam guardadas no servidor e o cookie leva só a chave (cifrada).
// Assim o logout invalida mesmo a sessão (SEG12): um cookie copiado antes deixa de servir.
// Reiniciar a API termina todas as sessões, o que com um só utilizador não é problema.
public sealed class SessoesEmMemoria(IMemoryCache cache, IOptions<OpcoesSessao> opcoes) : ITicketStore
{
    private const string Prefixo = "sessao:";

    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var chave = Guid.NewGuid().ToString("N");
        Guardar(chave, ticket);
        return Task.FromResult(chave);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        Guardar(key, ticket);
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key) =>
        Task.FromResult(cache.Get<AuthenticationTicket>(Prefixo + key));

    public Task RemoveAsync(string key)
    {
        cache.Remove(Prefixo + key);
        return Task.CompletedTask;
    }

    // A expiração aqui só serve para libertar memória; quem decide se a sessão
    // ainda é válida é o handler do cookie e o ValidadorSessao.
    private void Guardar(string chave, AuthenticationTicket ticket) =>
        cache.Set(Prefixo + chave, ticket, new MemoryCacheEntryOptions
        {
            SlidingExpiration = opcoes.Value.Inatividade,
            AbsoluteExpirationRelativeToNow = opcoes.Value.DuracaoMaxima,
        });
}

public static class ValidadorSessao
{
    // Corre em cada pedido com sessão (SEG12): duração máxima e security stamp.
    // Mudar a password (repor-password) muda o stamp e termina todas as sessões abertas.
    public static async Task ValidarAsync(CookieValidatePrincipalContext contexto)
    {
        var servicos = contexto.HttpContext.RequestServices;
        var opcoes = servicos.GetRequiredService<IOptions<OpcoesSessao>>().Value;
        var agora = servicos.GetRequiredService<TimeProvider>().GetUtcNow();
        var principal = contexto.Principal!;

        var parcial = principal.HasClaim(Sessao.ClaimEtapa, Sessao.Parcial);
        var maximo = parcial ? opcoes.DuracaoSessaoParcial : opcoes.DuracaoMaxima;
        var expirou = agora - Sessao.Inicio(principal) > maximo;

        var stamp = await servicos.GetRequiredService<Core.Autenticacao.IRepositorioUtilizador>()
            .ObterSecurityStampAsync(contexto.HttpContext.RequestAborted);

        if (expirou || stamp is null || !principal.HasClaim(Sessao.ClaimStamp, stamp))
        {
            contexto.RejectPrincipal();
            await contexto.HttpContext.SignOutAsync(Sessao.Esquema);
        }
    }
}
