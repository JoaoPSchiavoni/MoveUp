using MoveUp.Entities;

namespace MoveUp.Domain.Interfaces;

public interface IExercicioRepository
{
    Task<List<Exercicio>> ObterTodosAsync(CancellationToken ct = default);
    Task<Exercicio?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistemAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<bool> EmUsoAsync(Guid id, CancellationToken ct = default);
    Task SalvarAsync(Exercicio exercicio, CancellationToken ct = default);
    Task<bool> ExcluirAsync(Guid id, CancellationToken ct = default);
}
