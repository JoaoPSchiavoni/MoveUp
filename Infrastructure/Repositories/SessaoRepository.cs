using System.Data;
using Microsoft.EntityFrameworkCore;
using MoveUp.Context;
using MoveUp.Domain.Interfaces;
using MoveUp.Entities;

namespace MoveUp.Infrastructure.Repositories;

public class SessaoRepository(MoveUpDbContext db) : ISessaoRepository
{
    // Nested collections use one statement so reads cannot mix session and series
    // from different points in time during concurrent completion.
    private IQueryable<SessaoTreino> Completo => db.SessoesTreino.AsSingleQuery()
        .Include(x => x.Exercicios.OrderBy(e => e.Ordem))
        .ThenInclude(e => e.Series.OrderBy(s => s.Ordem));

    public Task<SessaoTreino?> ObterAsync(Guid id, CancellationToken ct) =>
        Completo.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<SessaoTreino?> ObterAtivaAsync(CancellationToken ct) =>
        Completo.SingleOrDefaultAsync(x => x.Status == StatusSessao.EmAndamento, ct);
    public Task<List<SessaoTreino>> HistoricoAsync(int skip, int take, CancellationToken ct) =>
        Completo.AsNoTracking().Where(x => x.Status == StatusSessao.Concluida)
            .OrderByDescending(x => x.Inicio).ThenByDescending(x => x.Id)
            .Skip(skip).Take(take).ToListAsync(ct);
    public void Adicionar(SessaoTreino sessao) => db.SessoesTreino.Add(sessao);
    public async Task<T> EmTransacaoAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        // SQLite's serializable transaction reserves the writer before reading state.
        // This serializes series updates against finalization and concurrent starts.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var result = await action();
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return result;
    }
}
