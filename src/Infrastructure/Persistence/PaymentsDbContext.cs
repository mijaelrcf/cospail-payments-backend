using Application.Interfaces.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// Contexto de persistencia para los cobros QR de la aplicación.
/// </summary>
public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
    : DbContext(options),
        IPaymentsDbContext
{
    /// <summary>
    /// QR de cobro emitidos por Banco Económico.
    /// </summary>
    public DbSet<PagoQr> PagosQr => Set<PagoQr>();

    /// <summary>
    /// Pagos agrupados de deudas de Cospail.
    /// </summary>
    public DbSet<PagoCospail> PagosCospail => Set<PagoCospail>();

    /// <summary>
    /// Deudas de Cospail incluidas en un pago.
    /// </summary>
    public DbSet<DeudaCospail> DeudasCospail => Set<DeudaCospail>();

    /// <summary>
    /// Notificaciones de pago QR recibidas de Banco Económico.
    /// </summary>
    public DbSet<NotificacionPagoQr> NotificacionesPagoQr => Set<NotificacionPagoQr>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurePagoQr(modelBuilder);
        ConfigurePagoCospail(modelBuilder);
        ConfigureDeudaCospail(modelBuilder);
        ConfigureNotificacionPagoQr(modelBuilder);
    }

    private static void ConfigurePagoQr(ModelBuilder modelBuilder)
    {
        var pagoQr = modelBuilder.Entity<PagoQr>();
        pagoQr.ToTable("pagos_qr");
        pagoQr.HasKey(x => x.Id);
        pagoQr.Property(x => x.Id).HasColumnName("id");
        pagoQr.Property(x => x.TransactionId).HasColumnName("transaction_id").HasMaxLength(100).IsRequired();
        pagoQr.Property(x => x.QrId).HasColumnName("qr_id").HasMaxLength(100).IsRequired();
        pagoQr.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        pagoQr.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        pagoQr.Property(x => x.DueDate).HasColumnName("due_date").HasColumnType("date").IsRequired();
        pagoQr.Property(x => x.SingleUse).HasColumnName("single_use").IsRequired();
        pagoQr.Property(x => x.ModifyAmount).HasColumnName("modify_amount").IsRequired();
        pagoQr.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        pagoQr.Property(x => x.BranchCode).HasColumnName("branch_code").HasMaxLength(5);
        pagoQr.Property(x => x.QrImage).HasColumnName("qr_image");
        pagoQr.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        pagoQr.Property(x => x.PaidAtUtc).HasColumnName("paid_at_utc").HasColumnType("timestamp with time zone");
        pagoQr.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();
        pagoQr.HasIndex(x => x.TransactionId).IsUnique();
        pagoQr.HasIndex(x => x.QrId).IsUnique();
        pagoQr.HasIndex(x => x.Status);
        pagoQr.HasIndex(x => x.CreatedAtUtc);
    }

    private static void ConfigurePagoCospail(ModelBuilder modelBuilder)
    {
        var pagoCospail = modelBuilder.Entity<PagoCospail>();
        pagoCospail.ToTable("pagos_cospail");
        pagoCospail.HasKey(x => x.Id);
        pagoCospail.Property(x => x.Id).HasColumnName("id");
        pagoCospail.Property(x => x.FixedCode).HasColumnName("fixed_code").IsRequired();
        pagoCospail.Property(x => x.DocumentId).HasColumnName("document_id").HasMaxLength(32).IsRequired();
        pagoCospail.Property(x => x.MemberName).HasColumnName("member_name").HasMaxLength(200);
        pagoCospail.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2).IsRequired();
        pagoCospail.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(24).IsRequired();
        pagoCospail.Property(x => x.PagoQrId).HasColumnName("pago_qr_id").HasColumnType("uuid");
        pagoCospail.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        pagoCospail.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
        pagoCospail.HasOne(x => x.Qr)
            .WithMany()
            .HasForeignKey(x => x.PagoQrId)
            .IsRequired(false);
        pagoCospail.HasIndex(x => x.FixedCode);
        pagoCospail.HasIndex(x => x.Status);
        pagoCospail.HasIndex(x => x.PagoQrId).IsUnique();
    }

    private static void ConfigureDeudaCospail(ModelBuilder modelBuilder)
    {
        var deudaCospail = modelBuilder.Entity<DeudaCospail>();
        deudaCospail.ToTable("deudas_cospail");
        deudaCospail.HasKey(x => x.Id);
        deudaCospail.Property(x => x.Id).HasColumnName("id");
        deudaCospail.Property(x => x.FixedCode).HasColumnName("fixed_code").IsRequired();
        deudaCospail.Property(x => x.DocumentId).HasColumnName("document_id").HasMaxLength(32).IsRequired();
        deudaCospail.Property(x => x.MemberName).HasColumnName("member_name").HasMaxLength(200);
        deudaCospail.Property(x => x.CreditNumber).HasColumnName("credit_number").IsRequired();
        deudaCospail.Property(x => x.Type).HasColumnName("type").IsRequired();
        deudaCospail.Property(x => x.NoticeNumber).HasColumnName("notice_number").IsRequired();
        deudaCospail.Property(x => x.Year).HasColumnName("year").IsRequired();
        deudaCospail.Property(x => x.Month).HasColumnName("month").IsRequired();
        deudaCospail.Property(x => x.Period).HasColumnName("period").HasMaxLength(50).IsRequired();
        deudaCospail.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        deudaCospail.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(24).IsRequired();
        deudaCospail.Property(x => x.PagoCospailId).HasColumnName("pago_cospail_id").IsRequired();
        deudaCospail.HasOne(x => x.PagoCospail)
            .WithMany(x => x.Deudas)
            .HasForeignKey(x => x.PagoCospailId)
            .OnDelete(DeleteBehavior.Cascade);
        deudaCospail.HasIndex(x => new { x.FixedCode, x.CreditNumber, x.Type, x.Status });
    }

    private static void ConfigureNotificacionPagoQr(ModelBuilder modelBuilder)
    {
        var notificacion = modelBuilder.Entity<NotificacionPagoQr>();
        notificacion.ToTable("notificaciones_pago_qr");
        notificacion.HasKey(x => x.Id);
        notificacion.Property(x => x.Id).HasColumnName("id");
        notificacion.Property(x => x.PagoQrId).HasColumnName("pago_qr_id");
        notificacion.Property(x => x.QrId).HasColumnName("qr_id").HasMaxLength(100).IsRequired();
        notificacion.Property(x => x.TransactionId).HasColumnName("transaction_id").HasMaxLength(100).IsRequired();
        notificacion.Property(x => x.PaymentDate).HasColumnName("payment_date").HasMaxLength(30).IsRequired();
        notificacion.Property(x => x.PaymentTime).HasColumnName("payment_time").HasMaxLength(10).IsRequired();
        notificacion.Property(x => x.PaymentAtUtc).HasColumnName("payment_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        notificacion.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        notificacion.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        notificacion.Property(x => x.SenderBankCode).HasColumnName("sender_bank_code").HasMaxLength(32).IsRequired();
        notificacion.Property(x => x.SenderName).HasColumnName("sender_name").HasMaxLength(200).IsRequired();
        notificacion.Property(x => x.SenderDocumentId).HasColumnName("sender_document_id").HasMaxLength(50).IsRequired();
        notificacion.Property(x => x.SenderAccount).HasColumnName("sender_account").HasMaxLength(50).IsRequired();
        notificacion.Property(x => x.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        notificacion.Property(x => x.BranchCode).HasColumnName("branch_code").HasMaxLength(5);
        notificacion.Property(x => x.ReceivedAtUtc).HasColumnName("received_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        notificacion.HasOne(x => x.Qr)
            .WithMany()
            .HasForeignKey(x => x.PagoQrId)
            .IsRequired();
        notificacion.HasIndex(x => x.PagoQrId);
        notificacion.HasIndex(x => x.QrId);
        notificacion.HasIndex(x => x.TransactionId);
        notificacion.HasIndex(x => x.ReceivedAtUtc);
    }
}
