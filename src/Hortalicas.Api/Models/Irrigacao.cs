namespace Hortalicas.Api.Models;

public class Irrigacao
{
    public Guid Id { get; set; }
    public Guid EstufaId { get; set; }
    public Estufa Estufa { get; set; } = null!;
    public DateOnly Data { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public int DuracaoMinutos { get; set; }
    public string? Observacao { get; set; }
}
