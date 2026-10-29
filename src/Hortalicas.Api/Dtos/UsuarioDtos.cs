using System.ComponentModel.DataAnnotations;
using Hortalicas.Api.Validation;

namespace Hortalicas.Api.Dtos;

/// <summary>Dados para cadastrar ou editar um usuário.</summary>
public record UsuarioRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 150 caracteres.")]
    public string? Nome { get; init; }

    /// <summary>CPF com ou sem máscara (ex.: 529.982.247-25).</summary>
    [Required(ErrorMessage = "CPF é obrigatório.")]
    [Cpf]
    public string? Cpf { get; init; }

    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(254, ErrorMessage = "E-mail deve ter no máximo 254 caracteres.")]
    public string? Email { get; init; }

    /// <summary>Telefone com DDD, com ou sem máscara (ex.: (11) 98765-4321).</summary>
    [Required(ErrorMessage = "Telefone é obrigatório.")]
    [Telefone]
    public string? Telefone { get; init; }
}

/// <summary>Usuário cadastrado. CPF e telefone são devolvidos só com dígitos.</summary>
public record UsuarioResponse(Guid Id, string Nome, string Cpf, string Email, string Telefone);
