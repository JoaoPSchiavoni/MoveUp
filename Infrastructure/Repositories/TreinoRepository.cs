using Microsoft.EntityFrameworkCore;
using MoveUp.Context;
using MoveUp.Domain.Interfaces;
using MoveUp.Entities;

namespace MoveUp.Infrastructure.Repositories;

public class TreinoRepository(MoveUpDbContext db) : ITreinoRepository
{
    private IQueryable<Treino> Completo => db.Treinos.AsNoTracking()
        .Include(t => t.TreinoExercicios.OrderBy(i => i.Ordem)).ThenInclude(i => i.Exercicio);

    public Task<List<Treino>> ObterTodosAsync(CancellationToken ct = default) =>
        Completo.OrderBy(t => t.DiaSemana).ThenBy(t => t.Nome).ToListAsync(ct);

    public Task<Treino?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        Completo.SingleOrDefaultAsync(t => t.Id == id, ct);

    public async Task SalvarAsync(Treino treino, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var existing = await db.Treinos.Include(t => t.TreinoExercicios)
            .SingleOrDefaultAsync(t => t.Id == treino.Id, ct);
        if (existing is null)
        {
            db.Treinos.Add(treino);
        }
        else
        {
            existing.Nome = treino.Nome;
            existing.Descricao = treino.Descricao;
            existing.DiaSemana = treino.DiaSemana;
            existing.Ativo = treino.Ativo;
            existing.UpdatedAt = DateTime.UtcNow;
            db.TreinoExercicios.RemoveRange(existing.TreinoExercicios);
            // Flush removals before inserting to respect the unique order/exercise indexes.
            // Both SaveChanges calls belong to this transaction.
            await db.SaveChangesAsync(ct);
            existing.TreinoExercicios.Clear();
            foreach (var item in treino.TreinoExercicios)
            {
                existing.TreinoExercicios.Add(item);
                db.TreinoExercicios.Add(item);
            }
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<bool> ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        var treino = await db.Treinos.FindAsync([id], ct);
        if (treino is null) return false;
        db.Treinos.Remove(treino);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
