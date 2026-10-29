using Hortalicas.Api.Data;
using Hortalicas.Api.Dtos;
using Hortalicas.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hortalicas.Api.Services;

public static class SituacaoCanteiros
{
    /// <summary>Dias depois da data prevista em que o canteiro ainda conta como pronto, antes de virar atrasado.</summary>
    public const int ToleranciaAtrasoDias = 3;

    public static SituacaoCanteiro Calcular(Plantio? ativo, DateOnly hoje)
    {
        if (ativo is null)
            return SituacaoCanteiro.Vazio;
        if (ativo.Colheitas.Count > 0)
            return SituacaoCanteiro.EmColheita;
        if (hoje < ativo.DataPrevistaColheita)
            return SituacaoCanteiro.EmCrescimento;
        return hoje <= ativo.DataPrevistaColheita.AddDays(ToleranciaAtrasoDias)
            ? SituacaoCanteiro.ProntoParaColher
            : SituacaoCanteiro.Atrasado;
    }
}

public interface IPainelService
{
    Task<IReadOnlyList<EstufaPainelResponse>> GeralAsync(CancellationToken ct = default);
    Task<MapaEstufaResponse?> MapaAsync(Guid estufaId, CancellationToken ct = default);
    Task<HojeResponse> HojeAsync(CancellationToken ct = default);
    Task<ResumoMesResponse> ResumoMesAsync(int ano, int mes, CancellationToken ct = default);
}

public class PainelService(AppDbContext db, IRelogio relogio) : IPainelService
{
    public async Task<IReadOnlyList<EstufaPainelResponse>> GeralAsync(CancellationToken ct = default)
    {
        var hoje = relogio.Hoje();
        var estufas = await CarregarEstufasAsync(null, ct);
        var irrigacoes = await UltimasIrrigacoesAsync(estufas, ct);

        var inicioMes = new DateOnly(hoje.Year, hoje.Month, 1);
        var colhidoNoMes = await db.Colheitas.AsNoTracking()
            .Where(c => c.Data >= inicioMes && c.Data <= hoje)
            .GroupBy(c => c.Plantio.Canteiro.EstufaId)
            .Select(g => new { EstufaId = g.Key, Total = g.Sum(c => c.PesoAproveitadoG) })
            .ToDictionaryAsync(x => x.EstufaId, x => x.Total, ct);

        return estufas.Select(estufa =>
        {
            var situacoes = estufa.Canteiros.Select(c => SituacaoCanteiros.Calcular(Ativo(c), hoje)).ToList();
            irrigacoes.TryGetValue(estufa.Id, out var ultima);
            return new EstufaPainelResponse(
                estufa.Id,
                estufa.Nome,
                estufa.AreaM2,
                estufa.Canteiros.Count,
                situacoes.Count(s => s == SituacaoCanteiro.Vazio),
                situacoes.Count(s => s == SituacaoCanteiro.EmCrescimento),
                situacoes.Count(s => s == SituacaoCanteiro.ProntoParaColher),
                situacoes.Count(s => s == SituacaoCanteiro.EmColheita),
                situacoes.Count(s => s == SituacaoCanteiro.Atrasado),
                estufa.Canteiros.Select(Ativo).OfType<Plantio>().Select(p => p.Hortalica.Nome)
                    .Distinct().Order(StringComparer.CurrentCultureIgnoreCase).ToList(),
                UltimaIrrigacao(ultima, hoje),
                IrrigacaoAtrasada(estufa, ultima, hoje),
                colhidoNoMes.GetValueOrDefault(estufa.Id));
        }).ToList();
    }

    public async Task<MapaEstufaResponse?> MapaAsync(Guid estufaId, CancellationToken ct = default)
    {
        var hoje = relogio.Hoje();
        var estufa = (await CarregarEstufasAsync(estufaId, ct)).FirstOrDefault();
        if (estufa is null)
            return null;

        var irrigacoes = await UltimasIrrigacoesAsync([estufa], ct);
        irrigacoes.TryGetValue(estufa.Id, out var ultima);
        var areaCanteiro = estufa.Canteiros.Count == 0 ? 0 : Math.Round(estufa.AreaM2 / estufa.Canteiros.Count, 1);

        var canteiros = estufa.Canteiros.OrderBy(c => c.Numero).Select(canteiro =>
        {
            var ativo = Ativo(canteiro);
            var plantio = ativo is null
                ? null
                : new PlantioAtivoResponse(
                    ativo.Id,
                    ativo.HortalicaId,
                    ativo.Hortalica.Nome,
                    ativo.DataPlantio,
                    ativo.DataPrevistaColheita,
                    hoje.DayNumber - ativo.DataPlantio.DayNumber,
                    ativo.DataPrevistaColheita.DayNumber - hoje.DayNumber,
                    ativo.Colheitas.Sum(c => c.PesoAproveitadoG),
                    ativo.Colheitas.Count);
            return new CanteiroMapaResponse(
                canteiro.Id, canteiro.Numero, areaCanteiro, SituacaoCanteiros.Calcular(ativo, hoje), plantio);
        }).ToList();

        return new MapaEstufaResponse(
            EstufaService.ParaResponse(estufa),
            UltimaIrrigacao(ultima, hoje),
            IrrigacaoAtrasada(estufa, ultima, hoje),
            canteiros);
    }

    public async Task<HojeResponse> HojeAsync(CancellationToken ct = default)
    {
        var hoje = relogio.Hoje();
        var estufas = await CarregarEstufasAsync(null, ct);
        var irrigacoes = await UltimasIrrigacoesAsync(estufas, ct);

        var paraColher = estufas
            .SelectMany(estufa => estufa.Canteiros.Select(canteiro => (estufa, canteiro, ativo: Ativo(canteiro))))
            .Where(x => x.ativo is not null)
            .Select(x => new CanteiroHojeResponse(
                x.estufa.Id,
                x.estufa.Nome,
                x.canteiro.Id,
                x.canteiro.Numero,
                x.ativo!.Id,
                x.ativo.Hortalica.Nome,
                SituacaoCanteiros.Calcular(x.ativo, hoje),
                x.ativo.DataPrevistaColheita,
                Math.Max(0, hoje.DayNumber - x.ativo.DataPrevistaColheita.DayNumber)))
            .Where(c => c.Situacao is SituacaoCanteiro.ProntoParaColher or SituacaoCanteiro.Atrasado or SituacaoCanteiro.EmColheita)
            .OrderBy(c => c.Situacao switch
            {
                SituacaoCanteiro.Atrasado => 0,
                SituacaoCanteiro.ProntoParaColher => 1,
                _ => 2,
            })
            .ThenByDescending(c => c.DiasDeAtraso)
            .ThenBy(c => c.EstufaNome, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(c => c.CanteiroNumero)
            .ToList();

        var semIrrigacao = estufas
            .Select(estufa => (estufa, ultima: irrigacoes.GetValueOrDefault(estufa.Id)))
            .Where(x => IrrigacaoAtrasada(x.estufa, x.ultima, hoje))
            .Select(x => new EstufaSemIrrigacaoResponse(
                x.estufa.Id, x.estufa.Nome, UltimaIrrigacao(x.ultima, hoje), x.estufa.DiasMaxSemIrrigacao))
            .ToList();

        return new HojeResponse(hoje, paraColher, semIrrigacao);
    }

    public async Task<ResumoMesResponse> ResumoMesAsync(int ano, int mes, CancellationToken ct = default)
    {
        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);

        var colheitas = await db.Colheitas.AsNoTracking()
            .Where(c => c.Data >= inicio && c.Data <= fim)
            .Select(c => new
            {
                c.PesoAproveitadoG,
                c.PesoDescartadoG,
                c.Plantio.HortalicaId,
                HortalicaNome = c.Plantio.Hortalica.Nome,
                c.Plantio.Canteiro.EstufaId,
            })
            .ToListAsync(ct);
        var irrigacoes = await db.Irrigacoes.AsNoTracking()
            .Where(i => i.Data >= inicio && i.Data <= fim)
            .Select(i => new { i.EstufaId, i.DuracaoMinutos })
            .ToListAsync(ct);
        var estufas = await db.Estufas.AsNoTracking().ToListAsync(ct);

        var aproveitado = colheitas.Sum(c => c.PesoAproveitadoG);
        var descartado = colheitas.Sum(c => c.PesoDescartadoG);

        var porHortalica = colheitas
            .GroupBy(c => (c.HortalicaId, c.HortalicaNome))
            .Select(g => new ColhidoPorHortalicaResponse(
                g.Key.HortalicaId, g.Key.HortalicaNome, g.Sum(c => c.PesoAproveitadoG), g.Sum(c => c.PesoDescartadoG)))
            .OrderByDescending(h => h.AproveitadoG)
            .ToList();

        var porEstufa = estufas
            .OrderBy(e => e.Nome, StringComparer.CurrentCultureIgnoreCase)
            .Select(estufa =>
            {
                var daEstufa = colheitas.Where(c => c.EstufaId == estufa.Id).ToList();
                var irrigadas = irrigacoes.Where(i => i.EstufaId == estufa.Id).ToList();
                var colhido = daEstufa.Sum(c => c.PesoAproveitadoG);
                return new ResumoEstufaResponse(
                    estufa.Id,
                    estufa.Nome,
                    estufa.AreaM2,
                    colhido,
                    daEstufa.Sum(c => c.PesoDescartadoG),
                    estufa.AreaM2 > 0 ? Math.Round(colhido / estufa.AreaM2, 1) : 0,
                    irrigadas.Count,
                    irrigadas.Sum(i => i.DuracaoMinutos));
            })
            .ToList();

        return new ResumoMesResponse(
            ano,
            mes,
            aproveitado,
            descartado,
            aproveitado + descartado > 0 ? Math.Round(100.0 * descartado / (aproveitado + descartado), 1) : 0,
            colheitas.Count,
            porHortalica,
            porEstufa);
    }

    private async Task<List<Estufa>> CarregarEstufasAsync(Guid? estufaId, CancellationToken ct)
    {
        var consulta = db.Estufas.AsNoTracking()
            .Include(e => e.Canteiros)
                .ThenInclude(c => c.Plantios.Where(p => p.Situacao == SituacaoPlantio.EmAndamento))
                .ThenInclude(p => p.Hortalica)
            .Include(e => e.Canteiros)
                .ThenInclude(c => c.Plantios.Where(p => p.Situacao == SituacaoPlantio.EmAndamento))
                .ThenInclude(p => p.Colheitas)
            .AsSplitQuery();
        if (estufaId is not null)
            consulta = consulta.Where(e => e.Id == estufaId);

        var estufas = await consulta.ToListAsync(ct);
        return estufas.OrderBy(e => e.Nome, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private async Task<Dictionary<Guid, Irrigacao>> UltimasIrrigacoesAsync(IEnumerable<Estufa> estufas, CancellationToken ct)
    {
        var ultimas = new Dictionary<Guid, Irrigacao>();
        foreach (var estufa in estufas)
        {
            var ultima = await db.Irrigacoes.AsNoTracking()
                .Where(i => i.EstufaId == estufa.Id)
                .OrderByDescending(i => i.Data).ThenByDescending(i => i.HoraInicio)
                .FirstOrDefaultAsync(ct);
            if (ultima is not null)
                ultimas[estufa.Id] = ultima;
        }
        return ultimas;
    }

    private static Plantio? Ativo(Canteiro canteiro) =>
        canteiro.Plantios.FirstOrDefault(p => p.Situacao == SituacaoPlantio.EmAndamento);

    private static UltimaIrrigacaoResponse? UltimaIrrigacao(Irrigacao? irrigacao, DateOnly hoje) =>
        irrigacao is null
            ? null
            : new UltimaIrrigacaoResponse(irrigacao.Data, irrigacao.DuracaoMinutos, hoje.DayNumber - irrigacao.Data.DayNumber);

    /// <summary>
    /// Só estufa com plantio em andamento entra em alerta. Conta os dias desde a última irrigação ou,
    /// se nunca foi irrigada, desde o plantio mais antigo em andamento.
    /// </summary>
    private static bool IrrigacaoAtrasada(Estufa estufa, Irrigacao? ultima, DateOnly hoje)
    {
        var ativos = estufa.Canteiros.Select(Ativo).OfType<Plantio>().ToList();
        if (ativos.Count == 0)
            return false;

        var referencia = ultima?.Data ?? ativos.Min(p => p.DataPlantio);
        return hoje.DayNumber - referencia.DayNumber > estufa.DiasMaxSemIrrigacao;
    }
}
