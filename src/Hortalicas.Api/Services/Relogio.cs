namespace Hortalicas.Api.Services;

/// <summary>Data de hoje no fuso de Brasília, usada nas regras de prazo (plantio, colheita, irrigação).</summary>
public interface IRelogio
{
    DateOnly Hoje();
}

public class Relogio(TimeProvider tempo) : IRelogio
{
    private static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public DateOnly Hoje() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(tempo.GetUtcNow(), Fuso).DateTime);
}
