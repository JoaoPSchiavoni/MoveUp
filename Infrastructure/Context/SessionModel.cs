using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MoveUp.Entities;

namespace MoveUp.Context;

internal static class SessionModel
{
    public static void Configure(ModelBuilder model)
    {
        var utc = new ValueConverter<DateTime, DateTime>(value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        model.Entity<SessaoTreino>(entity =>
        {
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.NomeTreino).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Observacao).HasMaxLength(2000);
            entity.Property(x => x.Inicio).HasConversion(utc);
            entity.Property(x => x.Fim).HasConversion(utc);
            entity.HasOne(x => x.Treino).WithMany().HasForeignKey(x => x.TreinoId)
                .OnDelete(DeleteBehavior.SetNull);
            // Single-user MVP: enforce at most one active session in the database itself.
            entity.HasIndex(x => x.Status).IsUnique().HasFilter("Status = 'emAndamento'");
            entity.HasIndex(x => new { x.Inicio, x.Id });
            entity.ToTable("SessoesTreino", table =>
            {
                table.HasCheckConstraint("CK_Sessao_Status", "Status IN ('emAndamento', 'concluida', 'cancelada')");
                table.HasCheckConstraint("CK_Sessao_Fim", "(Status = 'emAndamento' AND Fim IS NULL) OR (Status <> 'emAndamento' AND Fim IS NOT NULL AND Fim >= Inicio)");
                table.HasCheckConstraint("CK_Sessao_Nome", "length(trim(NomeTreino)) BETWEEN 1 AND 120");
                table.HasCheckConstraint("CK_Sessao_Observacao", "Observacao IS NULL OR length(Observacao) <= 2000");
            });
        });
        model.Entity<SessaoExercicio>(entity =>
        {
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.GrupoMuscular).HasMaxLength(80).IsRequired();
            entity.HasOne(x => x.SessaoTreino).WithMany(x => x.Exercicios)
                .HasForeignKey(x => x.SessaoTreinoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.SessaoTreinoId, x.Ordem }).IsUnique();
            entity.ToTable("SessaoExercicios", table =>
            {
                table.HasCheckConstraint("CK_SessaoExercicio_Ordem", "Ordem >= 0");
                table.HasCheckConstraint("CK_SessaoExercicio_Descanso", "TempoDescanso >= 0");
            });
        });
        model.Entity<SerieRealizada>(entity =>
        {
            entity.Property(x => x.ConcluidaEm).HasConversion(utc);
            entity.HasOne(x => x.SessaoExercicio).WithMany(x => x.Series)
                .HasForeignKey(x => x.SessaoExercicioId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.SessaoExercicioId, x.Ordem }).IsUnique();
            entity.ToTable("SeriesRealizadas", table =>
            {
                table.HasCheckConstraint("CK_Serie_Status", "Status IN ('pendente', 'concluida', 'pulada')");
                table.HasCheckConstraint("CK_Serie_Planejamento", "Ordem >= 0 AND RepeticoesPlanejadas > 0 AND CargaPlanejada >= 0");
                table.HasCheckConstraint("CK_Serie_Resultado", "(Status = 'concluida' AND Repeticoes IS NOT NULL AND Repeticoes > 0 AND Carga IS NOT NULL AND Carga >= 0 AND ConcluidaEm IS NOT NULL) OR (Status <> 'concluida' AND Repeticoes IS NULL AND Carga IS NULL AND ConcluidaEm IS NULL)");
            });
        });
    }
}
