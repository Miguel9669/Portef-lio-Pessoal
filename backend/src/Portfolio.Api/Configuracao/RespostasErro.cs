using Microsoft.AspNetCore.Mvc;
using Portfolio.Core.Comum;

namespace Portfolio.Api.Configuracao;

// Erros dos services em ProblemDetails (SEG26): 404, ou 400 com o campo do formulário em errors.
public static class RespostasErro
{
    public static ActionResult Para(this ControllerBase controller, Erro erro)
    {
        if (erro.Tipo == TipoErro.NaoEncontrado)
            return controller.Problem(title: erro.Mensagem, statusCode: StatusCodes.Status404NotFound);

        controller.ModelState.AddModelError(erro.Campo ?? "", erro.Mensagem);
        return controller.ValidationProblem(controller.ModelState);
    }
}
