namespace MoveUp.DTOs.Treino;

public sealed record TreinoCreateDto(
    string? Nome, int DiaSemana, string? Descricao, List<TreinoItemDto?>? Exercicios,
    bool Ativo = true);

public sealed record TreinoItemDto(
    Guid ExercicioId, int Series, int Repeticoes, double CargaInicial, int TempoDescanso);
