using System.ComponentModel.DataAnnotations;

namespace Hortalicas.Api.Dtos;

public record HortalicaRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 80 caracteres.")]
    public string? Nome { get; init; }

    /// <summary>Dias do plantio até a colheita.</summary>
    [Required(ErrorMessage = "Ciclo é obrigatório.")]
    [Range(1, 365, ErrorMessage = "Ciclo deve ficar entre 1 e 365 dias.")]
    public int? CicloDias { get; init; }

    [StringLength(500, ErrorMessage = "Observação deve ter no máximo 500 caracteres.")]
    public string? Observacao { get; init; }
}

public record HortalicaResponse(Guid Id, string Nome, int CicloDias, string? Observacao);
