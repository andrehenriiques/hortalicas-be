using System.ComponentModel.DataAnnotations;

namespace Hortalicas.Api.Validation;

/// <summary>Valida CPF (com ou sem máscara) pelos dígitos verificadores.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class CpfAttribute : ValidationAttribute
{
    public CpfAttribute() : base("CPF inválido.") { }

    public override bool IsValid(object? value) =>
        value is null || (value is string texto && Documentos.CpfValido(texto));
}

/// <summary>Valida telefone brasileiro com DDD: 10 ou 11 dígitos (com ou sem máscara).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class TelefoneAttribute : ValidationAttribute
{
    public TelefoneAttribute() : base("Telefone inválido. Informe DDD + número (10 ou 11 dígitos).") { }

    public override bool IsValid(object? value) =>
        value is null || (value is string texto && Documentos.TelefoneValido(texto));
}

public static class Documentos
{
    public static string SomenteDigitos(string valor) => new(valor.Where(char.IsDigit).ToArray());

    public static bool CpfValido(string cpf)
    {
        if (cpf.Any(c => !char.IsDigit(c) && c is not ('.' or '-' or ' ')))
            return false;

        var digitos = SomenteDigitos(cpf);
        if (digitos.Length != 11 || digitos.Distinct().Count() == 1)
            return false;

        return digitos[9] - '0' == DigitoVerificador(digitos, 9)
            && digitos[10] - '0' == DigitoVerificador(digitos, 10);
    }

    public static bool TelefoneValido(string telefone)
    {
        if (telefone.Any(c => !char.IsDigit(c) && c is not ('(' or ')' or '-' or ' ' or '+')))
            return false;

        var digitos = SomenteDigitos(telefone);
        return digitos.Length is 10 or 11;
    }

    private static int DigitoVerificador(string digitos, int quantidade)
    {
        var soma = 0;
        for (var i = 0; i < quantidade; i++)
            soma += (digitos[i] - '0') * (quantidade + 1 - i);

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
