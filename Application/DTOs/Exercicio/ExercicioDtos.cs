namespace MoveUp.DTOs.Exercicio;

public sealed record ExercicioSaveDto(string? Nome, string? GrupoMuscular, string? Descricao);
public sealed record ExercicioResponseDto(Guid Id, string Nome, string GrupoMuscular,
    string? Descricao, DateTime CreatedAt, DateTime? UpdatedAt)
{
    public static ExercicioResponseDto FromEntity(Entities.Exercicio e) =>
        new(e.Id, e.Nome, e.GrupoMuscular, e.Descricao, e.CreatedAt, e.UpdatedAt);
}
