namespace Hortalicas.Api.Models;

public class Hortalica
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    /// <summary>Dias do plantio até a colheita, informado pelo produtor.</summary>
    public int CicloDias { get; set; }

    public string? Observacao { get; set; }
}
