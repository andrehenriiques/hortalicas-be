using Hortalicas.Api.Dtos;
using Hortalicas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hortalicas.Api.Controllers;

[ApiController]
[Route("api/estufas")]
public class EstufasController(IEstufaService service, IPainelService painel) : ControllerBase
{
    /// <summary>Lista as estufas, ordenadas por nome.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EstufaResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await service.ListarAsync(ct));

    /// <summary>Obtém uma estufa pelo id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<EstufaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct)
    {
        var estufa = await service.ObterAsync(id, ct);
        return estufa is null ? this.Erro(Falha.NaoEncontrado("Estufa não encontrada.")) : Ok(estufa);
    }

    /// <summary>Visão de cima da estufa: canteiros com a situação e o plantio em andamento.</summary>
    [HttpGet("{id:guid}/mapa")]
    [ProducesResponseType<MapaEstufaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Mapa(Guid id, CancellationToken ct)
    {
        var mapa = await painel.MapaAsync(id, ct);
        return mapa is null ? this.Erro(Falha.NaoEncontrado("Estufa não encontrada.")) : Ok(mapa);
    }

    /// <summary>Cadastra uma estufa e cria os canteiros numerados de 1 a N.</summary>
    [HttpPost]
    [ProducesResponseType<EstufaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar(EstufaRequest request, CancellationToken ct) =>
        this.Resposta(await service.CriarAsync(request, ct),
            estufa => CreatedAtAction(nameof(Obter), new { id = estufa.Id }, estufa));

    /// <summary>Edita uma estufa. Reduzir canteiros só é possível se os retirados nunca tiveram plantio.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<EstufaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(Guid id, EstufaRequest request, CancellationToken ct) =>
        this.Resposta(await service.AtualizarAsync(id, request, ct), Ok);

    /// <summary>Exclui uma estufa sem histórico de plantio ou irrigação.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct) =>
        await service.ExcluirAsync(id, ct) is { } falha ? this.Erro(falha) : NoContent();
}
