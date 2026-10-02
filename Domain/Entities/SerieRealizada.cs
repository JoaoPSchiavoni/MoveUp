namespace MoveUp.Entities;

public class SerieRealizada
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessaoExercicioId { get; set; }
    public SessaoExercicio? SessaoExercicio { get; set; }
    public int Ordem { get; set; }
    public int RepeticoesPlanejadas { get; set; }
    public double CargaPlanejada { get; set; }
    public int? Repeticoes { get; set; }
    public double? Carga { get; set; }
    public string Status { get; set; } = StatusSerie.Pendente;
    public DateTime? ConcluidaEm { get; set; }
}
