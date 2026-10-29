using System.ComponentModel.DataAnnotations;
using Hortalicas.Api.Models;

namespace Hortalicas.Api.Dtos;

public record PlantioRequest
{
    [Required(ErrorMessage = "Canteiro é obrigatório.")]
    public Guid? CanteiroId { get; init; }

    [Required(ErrorMessage = "Hortaliça é obrigatória.")]
    public Guid? HortalicaId { get; init; }

    [Required(ErrorMessage = "Data do plantio é obrigatória.")]
    public DateOnly? DataPlantio { get; init; }

    [StringLength(500, ErrorMessage = "Observação deve ter no máximo 500 caracteres.")]
    public string? Observacao { get; init; }
}

/// <summary>Encerra o plantio sem colheita final (perda por praga, pendoamento, clima etc.).</summary>
public record EncerrarPlantioRequest
{
    [Required(ErrorMessage = "Data é obrigatória.")]
    public DateOnly? Data { get; init; }

    [Required(ErrorMessage = "Motivo é obrigatório.")]
    [StringLength(300, MinimumLength = 3, ErrorMessage = "Motivo deve ter entre 3 e 300 caracteres.")]
    public string? Motivo { get; init; }
}

public record PlantioResponse(
    Guid Id,
    Guid EstufaId,
    string EstufaNome,
    Guid CanteiroId,
    int CanteiroNumero,
    Guid HortalicaId,
    string HortalicaNome,
    DateOnly DataPlantio,
    DateOnly DataPrevistaColheita,
    SituacaoPlantio Situacao,
    DateOnly? DataEncerramento,
    string? MotivoEncerramento,
    string? Observacao,
    int ColhidoG,
    int DescartadoG,
    int QuantidadeColheitas);
