namespace Hortalicas.Api.Models;

public class Colheita
{
    public Guid Id { get; set; }
    public Guid PlantioId { get; set; }
    public Plantio Plantio { get; set; } = null!;
    public DateOnly Data { get; set; }
    public int PesoAproveitadoG { get; set; }
    public int PesoDescartadoG { get; set; }
    public string? MotivoDescarte { get; set; }

    /// <summary>Colheita final: encerrou o plantio e liberou o canteiro.</summary>
    public bool EncerrouCiclo { get; set; }

    /// <summary>Identificação do lote para rastreabilidade (data + código curto).</summary>
    public string Lote { get; set; } = string.Empty;
}
