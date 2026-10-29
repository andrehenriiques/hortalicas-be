using Hortalicas.Api.Dtos;
using Hortalicas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hortalicas.Api.Controllers;

[ApiController]
[Route("api/irrigacoes")]
public class IrrigacoesController(IIrrigacaoService service) : ControllerBase
{
    /// <summary>Lista as irrigações, da mais recente para a mais antiga.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<IrrigacaoResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] Guid? estufaId, [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate, CancellationToken ct) =>
        Ok(await service.ListarAsync(estufaId, de, ate, ct));

    /// <summary>Registra uma irrigação: estufa, data e quantos minutos ficou ligada.</summary>
    [HttpPost]
    [ProducesResponseType<IrrigacaoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(IrrigacaoRequest request, CancellationToken ct) =>
        this.Resposta(await service.CriarAsync(request, ct), irrigacao => StatusCode(StatusCodes.Status201Created, irrigacao));

    /// <summary>Exclui uma irrigação registrada por engano.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct) =>
        await service.ExcluirAsync(id, ct) is { } falha ? this.Erro(falha) : NoContent();
}
