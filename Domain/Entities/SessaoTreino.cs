namespace MoveUp.Entities;

public static class StatusSessao
{
    public const string EmAndamento = "emAndamento";
    public const string Concluida = "concluida";
    public const string Cancelada = "cancelada";
}
public static class StatusSerie
{
    public const string Pendente = "pendente";
    public const string Concluida = "concluida";
    public const string Pulada = "pulada";
}

public class SessaoTreino
{
    public Guid Id { get; set; }
    public Guid? TreinoId { get; set; }
    public Treino? Treino { get; set; }
    // Stable origin for idempotency, even after the original workout is deleted.
    public Guid TreinoOrigemId { get; set; }
    public string NomeTreino { get; set; } = "";
    public DateTime Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public string Status { get; set; } = StatusSessao.EmAndamento;
    public string? Observacao { get; set; }
    public DateOnly? DataPresenca { get; set; }
    public string? FusoPresenca { get; set; }
    public ICollection<SessaoExercicio> Exercicios { get; set; } = new List<SessaoExercicio>();
}
