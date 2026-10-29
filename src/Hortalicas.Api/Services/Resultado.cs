namespace Hortalicas.Api.Services;

public enum TipoFalha
{
    NaoEncontrado,
    Conflito,
    Invalido
}

/// <summary>Motivo de uma operação não ter sido feita. Invalido aponta o campo do pedido.</summary>
public sealed record Falha(TipoFalha Tipo, string Titulo, string? Detalhe = null, string? Campo = null)
{
    public static Falha NaoEncontrado(string titulo) => new(TipoFalha.NaoEncontrado, titulo);
    public static Falha Conflito(string titulo, string detalhe) => new(TipoFalha.Conflito, titulo, detalhe);
    public static Falha Invalido(string campo, string mensagem) => new(TipoFalha.Invalido, mensagem, Campo: campo);
}

public sealed record Resultado<T>(T? Valor, Falha? Falha) where T : class
{
    public static implicit operator Resultado<T>(T valor) => new(valor, null);
    public static implicit operator Resultado<T>(Falha falha) => new(null, falha);
}
