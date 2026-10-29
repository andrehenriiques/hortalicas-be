using Hortalicas.Api.Data;
using Hortalicas.Api.Dtos;
using Hortalicas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hortalicas.Api.Services;

public interface IPlantioService
{
    Task<IReadOnlyList<PlantioResponse>> ListarAsync(Guid? estufaId, SituacaoPlantio? situacao, CancellationToken ct = default);
    Task<PlantioResponse?> ObterAsync(Guid id, CancellationToken ct = default);
    Task<Resultado<PlantioResponse>> CriarAsync(PlantioRequest request, CancellationToken ct = default);
    Task<Resultado<PlantioResponse>> EncerrarAsync(Guid id, EncerrarPlantioRequest request, CancellationToken ct = default);
    Task<Falha?> ExcluirAsync(Guid id, CancellationToken ct = default);
}

public class PlantioService(AppDbContext db, IRelogio relogio) : IPlantioService
{
    public async Task<IReadOnlyList<PlantioResponse>> ListarAsync(Guid? estufaId, SituacaoPlantio? situacao, CancellationToken ct = default)
    {
        var consulta = Consulta();
        if (estufaId is not null)
            consulta = consulta.Where(p => p.Canteiro.EstufaId == estufaId);
        if (situacao is not null)
            consulta = consulta.Where(p => p.Situacao == situacao);

        var plantios = await consulta.OrderByDescending(p => p.DataPlantio).ToListAsync(ct);
        return plantios.Select(ParaResponse).ToList();
    }

    public async Task<PlantioResponse?> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var plantio = await Consulta().FirstOrDefaultAsync(p => p.Id == id, ct);
        return plantio is null ? null : ParaResponse(plantio);
    }

    public async Task<Resultado<PlantioResponse>> CriarAsync(PlantioRequest request, CancellationToken ct = default)
    {
        var canteiro = await db.Canteiros.Include(c => c.Estufa).FirstOrDefaultAsync(c => c.Id == request.CanteiroId, ct);
        if (canteiro is null)
            return Falha.Invalido(nameof(PlantioRequest.CanteiroId), "Canteiro não encontrado.");

        var hortalica = await db.Hortalicas.FirstOrDefaultAsync(h => h.Id == request.HortalicaId, ct);
        if (hortalica is null)
            return Falha.Invalido(nameof(PlantioRequest.HortalicaId), "Hortaliça não encontrada.");

        var data = request.DataPlantio!.Value;
        if (data > relogio.Hoje())
            return Falha.Invalido(nameof(PlantioRequest.DataPlantio), "A data do plantio não pode ser futura.");

        var ocupante = await db.Plantios.AsNoTracking().Include(p => p.Hortalica)
            .FirstOrDefaultAsync(p => p.CanteiroId == canteiro.Id && p.Situacao == SituacaoPlantio.EmAndamento, ct);
        if (ocupante is not null)
            return Falha.Conflito(
                "Canteiro ocupado.",
                $"O canteiro {canteiro.Numero} da {canteiro.Estufa.Nome} já tem {ocupante.Hortalica.Nome} plantada desde " +
                $"{ocupante.DataPlantio:dd/MM/yyyy}. Registre a colheita final ou encerre aquele plantio antes.");

        var plantio = new Plantio
        {
            Id = Guid.NewGuid(),
            CanteiroId = canteiro.Id,
            HortalicaId = hortalica.Id,
            DataPlantio = data,
            DataPrevistaColheita = data.AddDays(hortalica.CicloDias),
            Situacao = SituacaoPlantio.EmAndamento,
            Observacao = string.IsNullOrWhiteSpace(request.Observacao) ? null : request.Observacao.Trim(),
        };
        db.Plantios.Add(plantio);
        await db.SaveChangesAsync(ct);
        return (await ObterAsync(plantio.Id, ct))!;
    }

    public async Task<Resultado<PlantioResponse>> EncerrarAsync(Guid id, EncerrarPlantioRequest request, CancellationToken ct = default)
    {
        var plantio = await db.Plantios.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plantio is null)
            return NaoEncontrado();
        if (plantio.Situacao == SituacaoPlantio.Encerrado)
            return Falha.Conflito("Plantio já encerrado.", $"Este plantio foi encerrado em {plantio.DataEncerramento:dd/MM/yyyy}.");

        var data = request.Data!.Value;
        if (data < plantio.DataPlantio)
            return Falha.Invalido(nameof(EncerrarPlantioRequest.Data),
                $"A data não pode ser anterior ao plantio ({plantio.DataPlantio:dd/MM/yyyy}).");
        if (data > relogio.Hoje())
            return Falha.Invalido(nameof(EncerrarPlantioRequest.Data), "A data não pode ser futura.");

        plantio.Situacao = SituacaoPlantio.Encerrado;
        plantio.DataEncerramento = data;
        plantio.MotivoEncerramento = request.Motivo!.Trim();
        await db.SaveChangesAsync(ct);
        return (await ObterAsync(plantio.Id, ct))!;
    }

    public async Task<Falha?> ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        var plantio = await db.Plantios.Include(p => p.Colheitas).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plantio is null)
            return NaoEncontrado();
        if (plantio.Colheitas.Count > 0)
            return Falha.Conflito(
                "O plantio tem colheitas.",
                "Exclua as colheitas deste plantio antes. Para registrar uma perda, use Encerrar plantio.");

        db.Plantios.Remove(plantio);
        await db.SaveChangesAsync(ct);
        return null;
    }

    private IQueryable<Plantio> Consulta() => db.Plantios.AsNoTracking()
        .Include(p => p.Canteiro).ThenInclude(c => c.Estufa)
        .Include(p => p.Hortalica)
        .Include(p => p.Colheitas);

    private static Falha NaoEncontrado() => Falha.NaoEncontrado("Plantio não encontrado.");

    public static PlantioResponse ParaResponse(Plantio p) => new(
        p.Id, p.Canteiro.EstufaId, p.Canteiro.Estufa.Nome, p.CanteiroId, p.Canteiro.Numero,
        p.HortalicaId, p.Hortalica.Nome, p.DataPlantio, p.DataPrevistaColheita, p.Situacao,
        p.DataEncerramento, p.MotivoEncerramento, p.Observacao,
        p.Colheitas.Sum(c => c.PesoAproveitadoG), p.Colheitas.Sum(c => c.PesoDescartadoG), p.Colheitas.Count);
}
