using MoveUp.Entities;

namespace MoveUp.Infrastructure.Data;

public static class ExerciseCatalog
{
    public static readonly Exercicio[] Items = Create();
    private static Exercicio[] Create()
    {
        (string Nome, string Grupo)[] values = [
            ("Supino reto", "Peito"), ("Supino inclinado", "Peito"), ("Crucifixo", "Peito"),
            ("Puxada frontal", "Costas"), ("Remada baixa", "Costas"),
            ("Agachamento", "Pernas"), ("Leg press", "Pernas"), ("Cadeira extensora", "Pernas"),
            ("Rosca direta", "Bíceps"), ("Tríceps pulley", "Tríceps"),
            ("Desenvolvimento", "Ombros"), ("Abdominal", "Abdômen")
        ];
        return values.Select((v, i) => new Exercicio
        {
            Id = Guid.Parse($"00000000-0000-4000-8000-{i + 1:000000000000}"),
            Nome = v.Nome, GrupoMuscular = v.Grupo,
            CreatedAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
        }).ToArray();
    }
}
