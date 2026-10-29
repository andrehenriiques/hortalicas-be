using Hortalicas.Api.Dtos;
using Hortalicas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hortalicas.Api.Controllers;

[ApiController]
[Route("api/painel")]
public class PainelController(IPainelService painel, IRelogio relogio) : ControllerBase
{
    /// <summary>Um card por estufa: canteiros por situação, hortaliças plantadas, última irrigação e colhido no mês.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EstufaPainelResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Geral(CancellationToken ct) => Ok(await painel.GeralAsync(ct));

    /// <summary>O que pede atenção hoje: canteiros para colher e estufas com irrigação atrasada.</summary>
    [HttpGet("hoje")]
    [ProducesResponseType<HojeResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Hoje(CancellationToken ct) => Ok(await painel.HojeAsync(ct));

    /// <summary>Resumo do mês (padrão: mês atual): colhido por hortaliça e por estufa, descarte e irrigação.</summary>
    [HttpGet("resumo")]
    [ProducesResponseType<ResumoMesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Resumo([FromQuery] int? ano, [FromQuery] int? mes, CancellationToken ct)
    {
        var hoje = relogio.Hoje();
        if (mes is < 1 or > 12)
            return this.Erro(Falha.Invalido("mes", "Mês deve ficar entre 1 e 12."));
        if (ano is < 2000 or > 2100)
            return this.Erro(Falha.Invalido("ano", "Ano deve ficar entre 2000 e 2100."));

        return Ok(await painel.ResumoMesAsync(ano ?? hoje.Year, mes ?? hoje.Month, ct));
    }
}
