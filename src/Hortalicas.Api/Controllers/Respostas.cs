using Hortalicas.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Hortalicas.Api.Controllers;

public static class Respostas
{
    /// <summary>Converte a falha do serviço em ProblemDetails: 404, 409 ou 400 com o erro no campo.</summary>
    public static IActionResult Erro(this ControllerBase controller, Falha falha)
    {
        if (falha.Tipo == TipoFalha.Invalido)
        {
            var estado = new ModelStateDictionary();
            estado.AddModelError(falha.Campo ?? string.Empty, falha.Titulo);
            return controller.ValidationProblem(estado);
        }

        var status = falha.Tipo == TipoFalha.NaoEncontrado
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status409Conflict;
        return controller.Problem(statusCode: status, title: falha.Titulo, detail: falha.Detalhe);
    }

    public static IActionResult Resposta<T>(this ControllerBase controller, Resultado<T> resultado, Func<T, IActionResult> sucesso)
        where T : class =>
        resultado.Falha is null ? sucesso(resultado.Valor!) : controller.Erro(resultado.Falha);
}
