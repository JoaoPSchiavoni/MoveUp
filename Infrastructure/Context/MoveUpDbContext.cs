using Microsoft.EntityFrameworkCore;
using MoveUp.Entities;
using MoveUp.Infrastructure.Data;

namespace MoveUp.Context;

public class MoveUpDbContext(DbContextOptions<MoveUpDbContext> options) : DbContext(options)
{
    public DbSet<Treino> Treinos => Set<Treino>();
    public DbSet<Exercicio> Exercicios => Set<Exercicio>();
    public DbSet<TreinoExercicio> TreinoExercicios => Set<TreinoExercicio>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Treino>(entity =>
        {
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Descricao).HasMaxLength(2000);
            entity.HasIndex(x => x.DiaSemana);
            entity.ToTable("Treinos", table =>
            {
                table.HasCheckConstraint("CK_Treino_Dia", "DiaSemana BETWEEN 1 AND 7");
                table.HasCheckConstraint("CK_Treino_Nome", "length(trim(Nome)) BETWEEN 1 AND 120");
            });
        });
        model.Entity<Exercicio>(entity =>
        {
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.GrupoMuscular).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Descricao).HasMaxLength(2000);
            entity.ToTable("Exercicios", table =>
            {
                table.HasCheckConstraint("CK_Exercicio_Nome", "length(trim(Nome)) BETWEEN 1 AND 120");
                table.HasCheckConstraint("CK_Exercicio_Grupo", "length(trim(GrupoMuscular)) BETWEEN 1 AND 80");
            });
            entity.HasData(ExerciseCatalog.Items);
        });
        model.Entity<TreinoExercicio>(entity =>
        {
            entity.HasOne(x => x.Treino).WithMany(x => x.TreinoExercicios)
                .HasForeignKey(x => x.TreinoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Exercicio).WithMany(x => x.TreinoExercicios)
                .HasForeignKey(x => x.ExercicioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.TreinoId, x.ExercicioId }).IsUnique();
            entity.HasIndex(x => new { x.TreinoId, x.Ordem }).IsUnique();
            entity.ToTable("TreinoExercicios", table =>
            {
                table.HasCheckConstraint("CK_Item_Series", "Series > 0");
                table.HasCheckConstraint("CK_Item_Repeticoes", "Repeticoes > 0");
                table.HasCheckConstraint("CK_Item_Carga", "CargaInicial >= 0");
                table.HasCheckConstraint("CK_Item_Descanso", "TempoDescanso >= 0");
                table.HasCheckConstraint("CK_Item_Ordem", "Ordem >= 0");
            });
        });
    }
}
