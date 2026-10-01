using Microsoft.EntityFrameworkCore;
using MoveUp.Context;
using MoveUp.Domain.Interfaces;
using MoveUp.Entities;

namespace MoveUp.Infrastructure.Repositories;

public class ExercicioRepository(MoveUpDbContext db) : IExercicioRepository
{
    public Task<List<Exercicio>> ObterTodosAsync(CancellationToken ct = default) =>
        db.Exercicios.AsNoTracking().OrderBy(x => x.GrupoMuscular).ThenBy(x => x.Nome).ToListAsync(ct);
    public Task<Exercicio?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Exercicios.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<bool> ExistemAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var unique = ids.Distinct().ToArray();
        return await db.Exercicios.CountAsync(x => unique.Contains(x.Id), ct) == unique.Length;
    }
    public Task<bool> EmUsoAsync(Guid id, CancellationToken ct = default) =>
        db.TreinoExercicios.AnyAsync(x => x.ExercicioId == id, ct);
    public async Task SalvarAsync(Exercicio exercicio, CancellationToken ct = default)
    {
        var existing = await db.Exercicios.FindAsync([exercicio.Id], ct);
        if (existing is null) db.Exercicios.Add(exercicio);
        else
        {
            existing.Nome = exercicio.Nome;
            existing.GrupoMuscular = exercicio.GrupoMuscular;
            existing.Descricao = exercicio.Descricao;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }
    public async Task<bool> ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await db.Exercicios.FindAsync([id], ct);
        if (existing is null) return false;
        db.Exercicios.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
