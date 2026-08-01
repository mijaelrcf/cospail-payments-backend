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

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
        pagoQr.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        pagoQr.Property(x => x.PaidAtUtc).HasColumnName("paid_at_utc").HasColumnType("timestamp with time zone");
        pagoQr.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();
        pagoQr.HasIndex(x => x.TransactionId).IsUnique();
        pagoQr.HasIndex(x => x.QrId).IsUnique();
        pagoQr.HasIndex(x => x.Status);
        pagoQr.HasIndex(x => x.CreatedAtUtc);
    }
}
