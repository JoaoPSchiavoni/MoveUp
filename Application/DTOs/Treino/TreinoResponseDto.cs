using MoveUp.DTOs.Exercicio;

namespace MoveUp.DTOs.Treino;

public sealed record TreinoResponseDto(Guid Id, string Nome, string? Descricao,
    int DiaSemana, bool Ativo, DateTime CreatedAt, DateTime? UpdatedAt,
    IReadOnlyList<TreinoItemResponseDto> Exercicios)
{
    public static TreinoResponseDto FromEntity(Entities.Treino treino) => new(
        treino.Id, treino.Nome, treino.Descricao, treino.DiaSemana, treino.Ativo,
        treino.CreatedAt, treino.UpdatedAt,
        treino.TreinoExercicios.OrderBy(i => i.Ordem).Select(i => new TreinoItemResponseDto(
            i.Id, i.Ordem, ExercicioResponseDto.FromEntity(i.Exercicio!),
            i.Series, i.Repeticoes, i.CargaInicial, i.TempoDescanso)).ToArray());
}

public sealed record TreinoItemResponseDto(Guid Id, int Ordem, ExercicioResponseDto Exercicio,
    int Series, int Repeticoes, double CargaInicial, int TempoDescanso);
