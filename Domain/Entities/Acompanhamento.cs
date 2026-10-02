namespace MoveUp.Entities;

public class AcompanhamentoConfig
{
    public int Id { get; set; } = 1;
    public string Fuso { get; set; } = "UTC";
    public DateOnly Inicio { get; set; }
}
public class RotinaRevisao
{
    public int Id { get; set; }
    public DateOnly Inicio { get; set; }
    public string Dias { get; set; } = "";
    public int[] DiasSemana => Dias.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
}
public class MetaRevisao
{
    public int Id { get; set; }
    public DateOnly Inicio { get; set; }
    public int Dias { get; set; }
}
