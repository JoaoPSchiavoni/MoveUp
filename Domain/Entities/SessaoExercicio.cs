namespace MoveUp.Entities;

public class SessaoExercicio
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessaoTreinoId { get; set; }
    public SessaoTreino? SessaoTreino { get; set; }
    public string Nome { get; set; } = "";
    public string GrupoMuscular { get; set; } = "";
    public int Ordem { get; set; }
    public int TempoDescanso { get; set; }
    public ICollection<SerieRealizada> Series { get; set; } = new List<SerieRealizada>();
}
