using System.ComponentModel.DataAnnotations;

namespace Hortalicas.Api.Dtos;

public record ColheitaRequest
{
    [Required(ErrorMessage = "Plantio é obrigatório.")]
    public Guid? PlantioId { get; init; }

    [Required(ErrorMessage = "Data é obrigatória.")]
    public DateOnly? Data { get; init; }

    /// <summary>Peso que foi para venda ou consumo, em gramas.</summary>
    [Required(ErrorMessage = "Peso colhido é obrigatório.")]
    [Range(0, 100_000_000, ErrorMessage = "Peso colhido deve ficar entre 0 e 100.000.000 g.")]
    public int? PesoAproveitadoG { get; init; }

    /// <summary>Peso descartado na colheita, em gramas.</summary>
    [Range(0, 100_000_000, ErrorMessage = "Peso descartado deve ficar entre 0 e 100.000.000 g.")]
    public int PesoDescartadoG { get; init; }

    [StringLength(300, ErrorMessage = "Motivo do descarte deve ter no máximo 300 caracteres.")]
    public string? MotivoDescarte { get; init; }

    /// <summary>true = colheita final: encerra o plantio e libera o canteiro.</summary>
    [Required(ErrorMessage = "Informe se esta colheita encerra o ciclo.")]
    public bool? EncerraCiclo { get; init; }
}

public record ColheitaResponse(
    Guid Id,
    string Lote,
    Guid PlantioId,
    Guid EstufaId,
    string EstufaNome,
    int CanteiroNumero,
    string HortalicaNome,
    DateOnly Data,
    int PesoAproveitadoG,
    int PesoDescartadoG,
    string? MotivoDescarte,
    bool EncerrouCiclo);
