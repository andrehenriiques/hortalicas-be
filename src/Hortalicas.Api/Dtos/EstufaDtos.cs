using System.ComponentModel.DataAnnotations;

namespace Hortalicas.Api.Dtos;

/// <summary>Dados para cadastrar ou editar uma estufa. Os canteiros são criados (ou retirados) a partir da quantidade.</summary>
public record EstufaRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 80 caracteres.")]
    public string? Nome { get; init; }

    [Required(ErrorMessage = "Comprimento é obrigatório.")]
    [Range(1, 1000, ErrorMessage = "Comprimento deve ficar entre 1 e 1000 metros.")]
    public double? ComprimentoM { get; init; }

    [Required(ErrorMessage = "Largura é obrigatória.")]
    [Range(1, 200, ErrorMessage = "Largura deve ficar entre 1 e 200 metros.")]
    public double? LarguraM { get; init; }

    [Required(ErrorMessage = "Quantidade de canteiros é obrigatória.")]
    [Range(1, 60, ErrorMessage = "Quantidade de canteiros deve ficar entre 1 e 60.")]
    public int? QuantidadeCanteiros { get; init; }

    /// <summary>Dias sem irrigação que a estufa aguenta antes de entrar em alerta.</summary>
    [Required(ErrorMessage = "Informe em quantos dias sem irrigação a estufa entra em alerta.")]
    [Range(1, 30, ErrorMessage = "Dias sem irrigação deve ficar entre 1 e 30.")]
    public int? DiasMaxSemIrrigacao { get; init; }

    [StringLength(500, ErrorMessage = "Observação deve ter no máximo 500 caracteres.")]
    public string? Observacao { get; init; }
}

public record EstufaResponse(
    Guid Id,
    string Nome,
    double ComprimentoM,
    double LarguraM,
    double AreaM2,
    int QuantidadeCanteiros,
    int DiasMaxSemIrrigacao,
    string? Observacao);
