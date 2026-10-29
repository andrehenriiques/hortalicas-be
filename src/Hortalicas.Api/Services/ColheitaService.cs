using Hortalicas.Api.Data;
using Hortalicas.Api.Dtos;
using Hortalicas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hortalicas.Api.Services;

public interface IColheitaService
{
    Task<IReadOnlyList<ColheitaResponse>> ListarAsync(Guid? estufaId, Guid? hortalicaId, DateOnly? de, DateOnly? ate, CancellationToken ct = default);
    Task<Resultado<ColheitaResponse>> CriarAsync(ColheitaRequest request, CancellationToken ct = default);
    Task<Falha?> ExcluirAsync(Guid id, CancellationToken ct = default);
}

public class ColheitaService(AppDbContext db, IRelogio relogio) : IColheitaService
{
    public const string MotivoColheitaFinal = "Colheita final";

    public async Task<IReadOnlyList<ColheitaResponse>> ListarAsync(
        Guid? estufaId, Guid? hortalicaId, DateOnly? de, DateOnly? ate, CancellationToken ct = default)
    {
        var consulta = Consulta();
        if (estufaId is not null)
            consulta = consulta.Where(c => c.Plantio.Canteiro.EstufaId == estufaId);
        if (hortalicaId is not null)
            consulta = consulta.Where(c => c.Plantio.HortalicaId == hortalicaId);
        if (de is not null)
            consulta = consulta.Where(c => c.Data >= de);
        if (ate is not null)
            consulta = consulta.Where(c => c.Data <= ate);

        var colheitas = await consulta.OrderByDescending(c => c.Data).ThenBy(c => c.Lote).ToListAsync(ct);
        return colheitas.Select(ParaResponse).ToList();
    }

    public async Task<Resultado<ColheitaResponse>> CriarAsync(ColheitaRequest request, CancellationToken ct = default)
    {
        var plantio = await db.Plantios.Include(p => p.Hortalica).Include(p => p.Canteiro).ThenInclude(c => c.Estufa)
            .FirstOrDefaultAsync(p => p.Id == request.PlantioId, ct);
        if (plantio is null)
            return Falha.Invalido(nameof(ColheitaRequest.PlantioId), "Plantio não encontrado.");
        if (plantio.Situacao == SituacaoPlantio.Encerrado)
            return Falha.Conflito(
                "Plantio encerrado.",
                $"O plantio de {plantio.Hortalica.Nome} do canteiro {plantio.Canteiro.Numero} foi encerrado em " +
                $"{plantio.DataEncerramento:dd/MM/yyyy}. Registre um novo plantio antes de colher.");

        var data = request.Data!.Value;
        if (data < plantio.DataPlantio)
            return Falha.Invalido(nameof(ColheitaRequest.Data),
                $"A colheita não pode ser anterior ao plantio ({plantio.DataPlantio:dd/MM/yyyy}).");
        if (data > relogio.Hoje())
            return Falha.Invalido(nameof(ColheitaRequest.Data), "A data da colheita não pode ser futura.");

        var aproveitado = request.PesoAproveitadoG!.Value;
        if (aproveitado + request.PesoDescartadoG == 0)
            return Falha.Invalido(nameof(ColheitaRequest.PesoAproveitadoG), "Informe o peso colhido.");
        if (request.PesoDescartadoG > 0 && string.IsNullOrWhiteSpace(request.MotivoDescarte))
            return Falha.Invalido(nameof(ColheitaRequest.MotivoDescarte), "Informe o motivo do descarte.");

        var id = Guid.NewGuid();
        var colheita = new Colheita
        {
            Id = id,
            PlantioId = plantio.Id,
            Plantio = plantio,
            Data = data,
            PesoAproveitadoG = aproveitado,
            PesoDescartadoG = request.PesoDescartadoG,
            MotivoDescarte = request.PesoDescartadoG > 0 ? request.MotivoDescarte!.Trim() : null,
            EncerrouCiclo = request.EncerraCiclo!.Value,
            Lote = $"{data:yyyyMMdd}-{id.ToString("N")[..6].ToUpperInvariant()}",
        };

        if (colheita.EncerrouCiclo)
        {
            plantio.Situacao = SituacaoPlantio.Encerrado;
            plantio.DataEncerramento = data;
            plantio.MotivoEncerramento = MotivoColheitaFinal;
        }

        db.Colheitas.Add(colheita);
        await db.SaveChangesAsync(ct);
        return ParaResponse(colheita);
    }

    public async Task<Falha?> ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        var colheita = await db.Colheitas.Include(c => c.Plantio).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (colheita is null)
            return Falha.NaoEncontrado("Colheita não encontrada.");

        if (colheita.EncerrouCiclo)
        {
            var plantio = colheita.Plantio;
            var canteiroOcupado = await db.Plantios.AnyAsync(
                p => p.CanteiroId == plantio.CanteiroId && p.Id != plantio.Id && p.Situacao == SituacaoPlantio.EmAndamento, ct);
            if (canteiroOcupado)
                return Falha.Conflito(
                    "O canteiro já tem um novo plantio.",
                    "Esta foi a colheita final e o canteiro já recebeu outro plantio. Exclua o plantio novo antes.");

            plantio.Situacao = SituacaoPlantio.EmAndamento;
            plantio.DataEncerramento = null;
            plantio.MotivoEncerramento = null;
        }

        db.Colheitas.Remove(colheita);
        await db.SaveChangesAsync(ct);
        return null;
    }

    private IQueryable<Colheita> Consulta() => db.Colheitas.AsNoTracking()
        .Include(c => c.Plantio).ThenInclude(p => p.Hortalica)
        .Include(c => c.Plantio).ThenInclude(p => p.Canteiro).ThenInclude(c => c.Estufa);

    private static ColheitaResponse ParaResponse(Colheita c) => new(
        c.Id, c.Lote, c.PlantioId, c.Plantio.Canteiro.EstufaId, c.Plantio.Canteiro.Estufa.Nome, c.Plantio.Canteiro.Numero,
        c.Plantio.Hortalica.Nome, c.Data, c.PesoAproveitadoG, c.PesoDescartadoG, c.MotivoDescarte, c.EncerrouCiclo);
}
