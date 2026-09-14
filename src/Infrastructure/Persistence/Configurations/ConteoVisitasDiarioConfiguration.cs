using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de <see cref="ConteoVisitasDiario"/> a la tabla <c>conteo_visitas_diario</c>.
/// </summary>
public sealed class ConteoVisitasDiarioConfiguration : IEntityTypeConfiguration<ConteoVisitasDiario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConteoVisitasDiario> builder)
    {
        builder.ToTable("conteo_visitas_diario");
        builder.HasKey(x => x.Fecha);
        builder.Property(x => x.Fecha).HasColumnName("fecha").HasColumnType("date").IsRequired();
        builder.Property(x => x.TotalVisitas).HasColumnName("total_visitas").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").IsRequired();
    }
}
