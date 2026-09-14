using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de <see cref="NotificacionPagoQr"/> a la tabla <c>notificaciones_pago_qr</c>.
/// </summary>
public sealed class NotificacionPagoQrConfiguration : IEntityTypeConfiguration<NotificacionPagoQr>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NotificacionPagoQr> builder)
    {
        builder.ToTable("notificaciones_pago_qr");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PagoQrId).HasColumnName("pago_qr_id");
        builder.Property(x => x.QrId).HasColumnName("qr_id").HasMaxLength(100).IsRequired();
        builder.Property(x => x.TransactionId).HasColumnName("transaction_id").HasMaxLength(100).IsRequired();
        builder.Property(x => x.PaymentDate).HasColumnName("payment_date").HasMaxLength(30).IsRequired();
        builder.Property(x => x.PaymentTime).HasColumnName("payment_time").HasMaxLength(10).IsRequired();
        builder.Property(x => x.PaymentAtUtc).HasColumnName("payment_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.SenderBankCode).HasColumnName("sender_bank_code").HasMaxLength(32).IsRequired();
        builder.Property(x => x.SenderName).HasColumnName("sender_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.SenderDocumentId).HasColumnName("sender_document_id").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SenderAccount).HasColumnName("sender_account").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        builder.Property(x => x.BranchCode).HasColumnName("branch_code").HasMaxLength(5);
        builder.Property(x => x.ReceivedAtUtc).HasColumnName("received_at_utc").HasColumnType("timestamp with time zone").IsRequired();

        builder
            .HasOne(x => x.Qr)
            .WithMany()
            .HasForeignKey(x => x.PagoQrId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PagoQrId);
        builder.HasIndex(x => x.QrId);
        builder.HasIndex(x => x.TransactionId);
        builder.HasIndex(x => x.ReceivedAtUtc);
    }
}
