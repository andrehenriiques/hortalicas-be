using Hortalicas.Api.Dtos;
using Hortalicas.Api.Models;
using Hortalicas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hortalicas.Api.Controllers;

[ApiController]
[Route("api/plantios")]
public class PlantiosController(IPlantioService service) : ControllerBase
{
    /// <summary>Lista os plantios, do mais recente para o mais antigo.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PlantioResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] Guid? estufaId, [FromQuery] SituacaoPlantio? situacao, CancellationToken ct) =>
        Ok(await service.ListarAsync(estufaId, situacao, ct));

    /// <summary>Obtém um plantio pelo id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PlantioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct)
    {
        var plantio = await service.ObterAsync(id, ct);
        return plantio is null ? this.Erro(Falha.NaoEncontrado("Plantio não encontrado.")) : Ok(plantio);
    }

    /// <summary>Registra um plantio num canteiro livre. A colheita prevista sai do ciclo da hortaliça.</summary>
    [HttpPost]
    [ProducesResponseType<PlantioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar(PlantioRequest request, CancellationToken ct) =>
        this.Resposta(await service.CriarAsync(request, ct),
            plantio => CreatedAtAction(nameof(Obter), new { id = plantio.Id }, plantio));

    /// <summary>Encerra o plantio sem colheita final (perda) e libera o canteiro.</summary>
    [HttpPost("{id:guid}/encerrar")]
    [ProducesResponseType<PlantioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Encerrar(Guid id, EncerrarPlantioRequest request, CancellationToken ct) =>
        this.Resposta(await service.EncerrarAsync(id, request, ct), Ok);

    /// <summary>Exclui um plantio registrado por engano (só sem colheitas).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct) =>
        await service.ExcluirAsync(id, ct) is { } falha ? this.Erro(falha) : NoContent();
}
