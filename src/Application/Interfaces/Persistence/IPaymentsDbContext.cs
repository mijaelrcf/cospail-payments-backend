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
    /// Persiste todos los cambios pendientes.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
