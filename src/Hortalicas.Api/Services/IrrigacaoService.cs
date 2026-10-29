using Hortalicas.Api.Data;
using Hortalicas.Api.Dtos;
using Hortalicas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hortalicas.Api.Services;

public interface IIrrigacaoService
{
    Task<IReadOnlyList<IrrigacaoResponse>> ListarAsync(Guid? estufaId, DateOnly? de, DateOnly? ate, CancellationToken ct = default);
    Task<Resultado<IrrigacaoResponse>> CriarAsync(IrrigacaoRequest request, CancellationToken ct = default);
    Task<Falha?> ExcluirAsync(Guid id, CancellationToken ct = default);
}

public class IrrigacaoService(AppDbContext db, IRelogio relogio) : IIrrigacaoService
{
    public async Task<IReadOnlyList<IrrigacaoResponse>> ListarAsync(Guid? estufaId, DateOnly? de, DateOnly? ate, CancellationToken ct = default)
    {
        var consulta = db.Irrigacoes.AsNoTracking().Include(i => i.Estufa).AsQueryable();
        if (estufaId is not null)
            consulta = consulta.Where(i => i.EstufaId == estufaId);
        if (de is not null)
            consulta = consulta.Where(i => i.Data >= de);
        if (ate is not null)
            consulta = consulta.Where(i => i.Data <= ate);

        var irrigacoes = await consulta
            .OrderByDescending(i => i.Data).ThenByDescending(i => i.HoraInicio)
            .ToListAsync(ct);
        return irrigacoes.Select(ParaResponse).ToList();
    }

    public async Task<Resultado<IrrigacaoResponse>> CriarAsync(IrrigacaoRequest request, CancellationToken ct = default)
    {
        var estufa = await db.Estufas.FirstOrDefaultAsync(e => e.Id == request.EstufaId, ct);
        if (estufa is null)
            return Falha.Invalido(nameof(IrrigacaoRequest.EstufaId), "Estufa não encontrada.");
        if (request.Data!.Value > relogio.Hoje())
            return Falha.Invalido(nameof(IrrigacaoRequest.Data), "A data da irrigação não pode ser futura.");

        var irrigacao = new Irrigacao
        {
            Id = Guid.NewGuid(),
            EstufaId = estufa.Id,
            Estufa = estufa,
            Data = request.Data.Value,
            HoraInicio = request.HoraInicio,
            DuracaoMinutos = request.DuracaoMinutos!.Value,
            Observacao = string.IsNullOrWhiteSpace(request.Observacao) ? null : request.Observacao.Trim(),
        };
        db.Irrigacoes.Add(irrigacao);
        await db.SaveChangesAsync(ct);
        return ParaResponse(irrigacao);
    }

    public async Task<Falha?> ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        var irrigacao = await db.Irrigacoes.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (irrigacao is null)
            return Falha.NaoEncontrado("Irrigação não encontrada.");

        db.Irrigacoes.Remove(irrigacao);
        await db.SaveChangesAsync(ct);
        return null;
    }

    private static IrrigacaoResponse ParaResponse(Irrigacao i) =>
        new(i.Id, i.EstufaId, i.Estufa.Nome, i.Data, i.HoraInicio, i.DuracaoMinutos, i.Observacao);
}
