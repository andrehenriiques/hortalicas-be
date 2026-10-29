using Hortalicas.Api.Validation;

namespace Hortalicas.Tests;

public class DocumentosTests
{
    [Theory]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    [InlineData("111.444.777-35")]
    public void CpfValido_aceita_cpf_com_ou_sem_mascara(string cpf) =>
        Assert.True(Documentos.CpfValido(cpf));

    [Theory]
    [InlineData("52998224724")]
    [InlineData("11111111111")]
    [InlineData("1234567890")]
    [InlineData("529982247250")]
    [InlineData("529a982247-25")]
    [InlineData("")]
    public void CpfValido_rejeita_cpf_invalido(string cpf) =>
        Assert.False(Documentos.CpfValido(cpf));

    [Theory]
    [InlineData("(11) 98765-4321")]
    [InlineData("11987654321")]
    [InlineData("1133334444")]
    public void TelefoneValido_aceita_ddd_mais_numero(string telefone) =>
        Assert.True(Documentos.TelefoneValido(telefone));

    [Theory]
    [InlineData("98765-4321")]
    [InlineData("119876543210")]
    [InlineData("11 9876x4321")]
    [InlineData("")]
    public void TelefoneValido_rejeita_telefone_invalido(string telefone) =>
        Assert.False(Documentos.TelefoneValido(telefone));
}
