namespace Hortalicas.Api.Models;

/// <summary>Faixa de cultivo da estufa, numerada de 1 a N no sentido da largura.</summary>
public class Canteiro
{
    public Guid Id { get; set; }
    public Guid EstufaId { get; set; }
    public Estufa Estufa { get; set; } = null!;
    public int Numero { get; set; }
    public List<Plantio> Plantios { get; set; } = [];
}
