using System.ComponentModel.DataAnnotations;

namespace Hortalicas.Api.Dtos;

public record IrrigacaoRequest
{
    [Required(ErrorMessage = "Estufa é obrigatória.")]
    public Guid? EstufaId { get; init; }

    [Required(ErrorMessage = "Data é obrigatória.")]
    public DateOnly? Data { get; init; }

    /// <summary>Hora em que a irrigação foi ligada (opcional), ex.: "06:30".</summary>
    public TimeOnly? HoraInicio { get; init; }

    [Required(ErrorMessage = "Informe quantos minutos a irrigação ficou ligada.")]
    [Range(1, 1440, ErrorMessage = "Duração deve ficar entre 1 e 1440 minutos.")]
    public int? DuracaoMinutos { get; init; }

    [StringLength(500, ErrorMessage = "Observação deve ter no máximo 500 caracteres.")]
    public string? Observacao { get; init; }
}

public record IrrigacaoResponse(
    Guid Id,
    Guid EstufaId,
    string EstufaNome,
    DateOnly Data,
    TimeOnly? HoraInicio,
    int DuracaoMinutos,
    string? Observacao);
