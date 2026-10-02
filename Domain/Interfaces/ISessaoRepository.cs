using MoveUp.Entities;

namespace MoveUp.Domain.Interfaces;

public interface ISessaoRepository
{
    Task<SessaoTreino?> ObterAsync(Guid id, CancellationToken ct);
    Task<SessaoTreino?> ObterAtivaAsync(CancellationToken ct);
    Task<List<SessaoTreino>> HistoricoAsync(int skip, int take, CancellationToken ct);
    void Adicionar(SessaoTreino sessao);
    Task<T> EmTransacaoAsync<T>(Func<Task<T>> action, CancellationToken ct);
}
