using MoveUp.Entities;

namespace MoveUp.Domain.Interfaces;

public interface ITreinoRepository
{
    Task<List<Treino>> ObterTodosAsync(CancellationToken ct = default);
    Task<Treino?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task SalvarAsync(Treino treino, CancellationToken ct = default);
    Task<bool> ExcluirAsync(Guid id, CancellationToken ct = default);
}
