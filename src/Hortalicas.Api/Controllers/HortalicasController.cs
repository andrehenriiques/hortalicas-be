using Hortalicas.Api.Dtos;
using Hortalicas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hortalicas.Api.Controllers;

[ApiController]
[Route("api/hortalicas")]
public class HortalicasController(IHortalicaService service) : ControllerBase
{
    /// <summary>Lista as hortaliças, ordenadas por nome.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<HortalicaResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await service.ListarAsync(ct));

    /// <summary>Obtém uma hortaliça pelo id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<HortalicaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct)
    {
        var hortalica = await service.ObterAsync(id, ct);
        return hortalica is null ? this.Erro(Falha.NaoEncontrado("Hortaliça não encontrada.")) : Ok(hortalica);
    }

    /// <summary>Cadastra uma hortaliça com o ciclo em dias.</summary>
    [HttpPost]
    [ProducesResponseType<HortalicaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar(HortalicaRequest request, CancellationToken ct) =>
        this.Resposta(await service.CriarAsync(request, ct),
            hortalica => CreatedAtAction(nameof(Obter), new { id = hortalica.Id }, hortalica));

    /// <summary>Edita uma hortaliça. Mudar o ciclo não altera a previsão dos plantios já registrados.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<HortalicaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(Guid id, HortalicaRequest request, CancellationToken ct) =>
        this.Resposta(await service.AtualizarAsync(id, request, ct), Ok);

    /// <summary>Exclui uma hortaliça que nunca foi plantada.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct) =>
        await service.ExcluirAsync(id, ct) is { } falha ? this.Erro(falha) : NoContent();
}
