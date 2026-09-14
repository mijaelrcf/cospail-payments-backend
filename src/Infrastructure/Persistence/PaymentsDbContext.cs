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

    /// <summary>
    /// Contadores diarios de visitas del frontend cliente.
    /// </summary>
    public DbSet<ConteoVisitasDiario> ConteosVisitasDiario => Set<ConteoVisitasDiario>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}
