using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces.Persistence;

/// <summary>
/// Abstracción del contexto de persistencia para la capa de aplicación.
/// </summary>
public interface IPaymentsDbContext
{
    /// <summary>
    /// QR de cobro emitidos por Banco Económico.
    /// </summary>
    DbSet<PagoQr> PagosQr { get; }

    /// <summary>
    /// Pagos agrupados de deudas de Cospail.
    /// </summary>
    DbSet<PagoCospail> PagosCospail { get; }

    /// <summary>
    /// Deudas de Cospail incluidas en un pago.
    /// </summary>
    DbSet<DeudaCospail> DeudasCospail { get; }

    /// <summary>
    /// Notificaciones de pago QR recibidas de Banco Económico.
    /// </summary>
    DbSet<NotificacionPagoQr> NotificacionesPagoQr { get; }

    /// <summary>
    /// Contadores diarios de visitas del frontend cliente (una fila por día).
    /// </summary>
    DbSet<ConteoVisitasDiario> ConteosVisitasDiario { get; }

    /// <summary>
    /// Persiste todos los cambios pendientes.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
