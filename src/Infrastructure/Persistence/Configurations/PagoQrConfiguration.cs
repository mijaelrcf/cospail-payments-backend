using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de <see cref="PagoQr"/> a la tabla <c>pagos_qr</c>.
/// </summary>
public sealed class PagoQrConfiguration : IEntityTypeConfiguration<PagoQr>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PagoQr> builder)
    {
        builder.ToTable("pagos_qr");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TransactionId).HasColumnName("transaction_id").HasMaxLength(100).IsRequired();
        builder.Property(x => x.QrId).HasColumnName("qr_id").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(x => x.DueDate).HasColumnName("due_date").HasColumnType("date").IsRequired();
        builder.Property(x => x.SingleUse).HasColumnName("single_use").IsRequired();
        builder.Property(x => x.ModifyAmount).HasColumnName("modify_amount").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(x => x.BranchCode).HasColumnName("branch_code").HasMaxLength(5);
        builder.Property(x => x.QrImage).HasColumnName("qr_image").HasColumnType("text");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.PaidAtUtc).HasColumnName("paid_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(x => x.TransactionId).IsUnique();
        builder.HasIndex(x => x.QrId).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAtUtc);

        // Búsqueda de QR vigente por socio: join PagosCospail -> Qr filtrando por estado y vencimiento.
        builder.HasIndex(x => new { x.Status, x.DueDate });
    }
}
