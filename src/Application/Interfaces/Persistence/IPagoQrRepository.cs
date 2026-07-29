using Domain.Entities;

namespace Application.Interfaces.Persistence;

/// <summary>
/// Abstrae el almacenamiento de QR de cobro emitidos por Banco Económico.
/// </summary>
public interface IPagoQrRepository
{
    /// <summary>
    /// Obtiene un QR por su identificador de transacción.
    /// </summary>
    Task<PagoQr?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un QR por el identificador emitido por Banco Económico.
    /// </summary>
    Task<PagoQr?> GetByQrIdAsync(string qrId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega un QR pendiente y persiste la operación.
    /// </summary>
    Task AddAsync(PagoQr pagoQr, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste los cambios realizados a un QR existente.
    /// </summary>
    Task UpdateAsync(PagoQr pagoQr, CancellationToken cancellationToken = default);
}
