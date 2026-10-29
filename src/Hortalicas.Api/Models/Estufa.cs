namespace Hortalicas.Api.Models;

public class Estufa
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public double ComprimentoM { get; set; }
    public double LarguraM { get; set; }

    /// <summary>Depois desse número de dias sem irrigação, a estufa com plantio em andamento entra em alerta.</summary>
    public int DiasMaxSemIrrigacao { get; set; }

    public string? Observacao { get; set; }
    public List<Canteiro> Canteiros { get; set; } = [];

    public double AreaM2 => ComprimentoM * LarguraM;
}
