namespace Hortalicas.Api.Dtos;

public enum SituacaoCanteiro
{
    Vazio,
    EmCrescimento,
    ProntoParaColher,
    EmColheita,
    Atrasado
}

public record UltimaIrrigacaoResponse(DateOnly Data, int DuracaoMinutos, int DiasAtras);

/// <summary>Card da estufa no painel geral.</summary>
public record EstufaPainelResponse(
    Guid Id,
    string Nome,
    double AreaM2,
    int TotalCanteiros,
    int CanteirosVazios,
    int CanteirosEmCrescimento,
    int CanteirosProntosParaColher,
    int CanteirosEmColheita,
    int CanteirosAtrasados,
    IReadOnlyList<string> HortalicasPlantadas,
    UltimaIrrigacaoResponse? UltimaIrrigacao,
    bool IrrigacaoAtrasada,
    int ColhidoNoMesG);

public record PlantioAtivoResponse(
    Guid Id,
    Guid HortalicaId,
    string HortalicaNome,
    DateOnly DataPlantio,
    DateOnly DataPrevistaColheita,
    int DiasDesdePlantio,
    int DiasParaColheita,
    int ColhidoG,
    int QuantidadeColheitas);

public record CanteiroMapaResponse(
    Guid Id,
    int Numero,
    double AreaM2,
    SituacaoCanteiro Situacao,
    PlantioAtivoResponse? Plantio);

/// <summary>Visão de cima da estufa: os canteiros lado a lado, cada um com a sua situação.</summary>
public record MapaEstufaResponse(
    EstufaResponse Estufa,
    UltimaIrrigacaoResponse? UltimaIrrigacao,
    bool IrrigacaoAtrasada,
    IReadOnlyList<CanteiroMapaResponse> Canteiros);

public record CanteiroHojeResponse(
    Guid EstufaId,
    string EstufaNome,
    Guid CanteiroId,
    int CanteiroNumero,
    Guid PlantioId,
    string HortalicaNome,
    SituacaoCanteiro Situacao,
    DateOnly DataPrevistaColheita,
    int DiasDeAtraso);

public record EstufaSemIrrigacaoResponse(
    Guid EstufaId,
    string EstufaNome,
    UltimaIrrigacaoResponse? UltimaIrrigacao,
    int DiasMaxSemIrrigacao);

/// <summary>O que pede atenção hoje: canteiros para colher e estufas com irrigação atrasada.</summary>
public record HojeResponse(
    DateOnly Data,
    IReadOnlyList<CanteiroHojeResponse> ParaColher,
    IReadOnlyList<EstufaSemIrrigacaoResponse> SemIrrigacao);

public record ColhidoPorHortalicaResponse(Guid HortalicaId, string HortalicaNome, int AproveitadoG, int DescartadoG);

public record ResumoEstufaResponse(
    Guid EstufaId,
    string EstufaNome,
    double AreaM2,
    int AproveitadoG,
    int DescartadoG,
    double GramasPorM2,
    int Irrigacoes,
    int MinutosIrrigacao);

public record ResumoMesResponse(
    int Ano,
    int Mes,
    int TotalAproveitadoG,
    int TotalDescartadoG,
    double PercentualDescarte,
    int QuantidadeColheitas,
    IReadOnlyList<ColhidoPorHortalicaResponse> PorHortalica,
    IReadOnlyList<ResumoEstufaResponse> PorEstufa);
