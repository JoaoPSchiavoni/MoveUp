using Microsoft.EntityFrameworkCore;
using MoveUp.Entities;

namespace MoveUp.Context;

internal static class ConsistencyModel
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<AcompanhamentoConfig>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Fuso).HasMaxLength(100);
            e.ToTable("AcompanhamentoConfig", t => t.HasCheckConstraint("CK_Acompanhamento_Unico", "Id = 1"));
        });
        model.Entity<RotinaRevisao>(e =>
        {
            e.Ignore(x => x.DiasSemana);
            e.Property(x => x.Dias).HasMaxLength(13);
            e.HasIndex(x => x.Inicio).IsUnique();
        });
        model.Entity<MetaRevisao>(e =>
        {
            e.HasIndex(x => x.Inicio).IsUnique();
            e.ToTable("MetaRevisoes", t => t.HasCheckConstraint("CK_Meta_Dias", "Dias BETWEEN 1 AND 7"));
        });
        model.Entity<SessaoTreino>(e =>
        {
            e.Property(x => x.FusoPresenca).HasMaxLength(100);
            e.HasIndex(x => new { x.Status, x.DataPresenca });
        });
    }
}
