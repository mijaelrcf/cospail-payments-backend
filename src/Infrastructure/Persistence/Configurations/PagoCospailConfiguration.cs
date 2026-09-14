using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de <see cref="PagoCospail"/> a la tabla <c>pagos_cospail</c>.
/// </summary>
public sealed class PagoCospailConfiguration : IEntityTypeConfiguration<PagoCospail>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PagoCospail> builder)
    {
        builder.ToTable("pagos_cospail");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FixedCode).HasColumnName("fixed_code").IsRequired();
        builder.Property(x => x.DocumentId).HasColumnName("document_id").HasMaxLength(32).IsRequired();
        builder.Property(x => x.MemberName).HasColumnName("member_name").HasMaxLength(200);
        builder.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(x => x.PagoQrId).HasColumnName("pago_qr_id").HasColumnType("uuid");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");

        builder
            .HasOne(x => x.Qr)
            .WithMany()
            .HasForeignKey(x => x.PagoQrId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Query caliente MemberHasActiveQrAsync: FixedCode + DocumentId + Status.
        builder.HasIndex(x => new { x.FixedCode, x.DocumentId, x.Status });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PagoQrId).IsUnique();
    }
}
