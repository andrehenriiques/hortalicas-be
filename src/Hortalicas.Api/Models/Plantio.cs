namespace Hortalicas.Api.Models;

public enum SituacaoPlantio
{
    EmAndamento,
    Encerrado
}

/// <summary>Ciclo de uma hortaliça num canteiro: do plantio até a colheita final ou o encerramento por perda.</summary>
public class Plantio
{
    public Guid Id { get; set; }
    public Guid CanteiroId { get; set; }
    public Canteiro Canteiro { get; set; } = null!;
    public Guid HortalicaId { get; set; }
    public Hortalica Hortalica { get; set; } = null!;
    public DateOnly DataPlantio { get; set; }

    /// <summary>Calculada no plantio (data + ciclo da hortaliça); não muda se o ciclo for editado depois.</summary>
    public DateOnly DataPrevistaColheita { get; set; }

    public SituacaoPlantio Situacao { get; set; }
    public DateOnly? DataEncerramento { get; set; }
    public string? MotivoEncerramento { get; set; }
    public string? Observacao { get; set; }
    public List<Colheita> Colheitas { get; set; } = [];
}
