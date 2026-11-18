using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Portfolio.Api.Autenticacao;

// SEG11: todos os pedidos de escrita (POST, PUT, DELETE…) têm de trazer o token CSRF no
// cabeçalho X-XSRF-TOKEN. Equivale ao AutoValidateAntiforgeryToken, que só existe com views.
public sealed class FiltroCsrf(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext contexto)
    {
        var metodo = contexto.HttpContext.Request.Method;
        if (HttpMethods.IsGet(metodo) || HttpMethods.IsHead(metodo) || HttpMethods.IsOptions(metodo) || HttpMethods.IsTrace(metodo))
            return;

        if (!await antiforgery.IsRequestValidAsync(contexto.HttpContext))
        {
            contexto.Result = new ObjectResult(new ProblemDetails
            {
                Title = "Token CSRF em falta ou inválido",
                Status = StatusCodes.Status400BadRequest,
            })
            { StatusCode = StatusCodes.Status400BadRequest };
        }
    }
}
