using Hortalicas.Api.Dtos;
using Hortalicas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hortalicas.Api.Controllers;

[ApiController]
[Route("api/colheitas")]
public class ColheitasController(IColheitaService service) : ControllerBase
{
    /// <summary>Lista as colheitas, da mais recente para a mais antiga.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ColheitaResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? estufaId, [FromQuery] Guid? hortalicaId, [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate,
        CancellationToken ct) =>
        Ok(await service.ListarAsync(estufaId, hortalicaId, de, ate, ct));

    /// <summary>Registra uma colheita (peso em gramas). Com encerraCiclo = true, encerra o plantio e libera o canteiro.</summary>
    [HttpPost]
    [ProducesResponseType<ColheitaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar(ColheitaRequest request, CancellationToken ct) =>
        this.Resposta(await service.CriarAsync(request, ct), colheita => StatusCode(StatusCodes.Status201Created, colheita));

    /// <summary>Exclui uma colheita registrada por engano. Se era a colheita final, o plantio volta a ficar em andamento.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct) =>
        await service.ExcluirAsync(id, ct) is { } falha ? this.Erro(falha) : NoContent();
}
