using Application.Interfaces.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// Repositorio EF Core para los QR de cobro.
/// </summary>
public sealed class PagoQrRepository(PaymentsDbContext dbContext) : IPagoQrRepository
{
    /// <inheritdoc />
    public Task<PagoQr?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default) =>
        dbContext.PagosQr.SingleOrDefaultAsync(x => x.TransactionId == transactionId, cancellationToken);

    /// <inheritdoc />
    public Task<PagoQr?> GetByQrIdAsync(string qrId, CancellationToken cancellationToken = default) =>
        dbContext.PagosQr.SingleOrDefaultAsync(x => x.QrId == qrId, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(PagoQr pagoQr, CancellationToken cancellationToken = default)
    {
        await dbContext.PagosQr.AddAsync(pagoQr, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PagoQr pagoQr, CancellationToken cancellationToken = default)
    {
        dbContext.PagosQr.Update(pagoQr);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
