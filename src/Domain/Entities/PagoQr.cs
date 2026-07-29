namespace Domain.Entities;

/// <summary>
/// Representa un QR de cobro emitido por Banco Económico y su estado de pago.
/// </summary>
public sealed class PagoQr
{
    /// <summary>
    /// Identificador interno del registro.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Identificador de transacción único en el sistema consumidor.
    /// </summary>
    public string TransactionId { get; private set; } = string.Empty;

    /// <summary>
    /// Identificador único del QR emitido por Banco Económico.
    /// </summary>
    public string QrId { get; private set; } = string.Empty;

    /// <summary>
    /// Importe solicitado para el QR.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Moneda del cobro.
    /// </summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>
    /// Fecha de vencimiento comunicada al banco.
    /// </summary>
    public DateOnly DueDate { get; private set; }

    /// <summary>
    /// Indica si el QR permite un único pago.
    /// </summary>
    public bool SingleUse { get; private set; }

    /// <summary>
    /// Indica si el banco puede aceptar un importe distinto al solicitado.
    /// </summary>
    public bool ModifyAmount { get; private set; }

    /// <summary>
    /// Descripción comercial del cobro, si fue proporcionada.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Sucursal comercial asociada, si fue proporcionada.
    /// </summary>
    public string? BranchCode { get; private set; }

    /// <summary>
    /// Fecha y hora UTC en que se registró la emisión del QR.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Fecha y hora UTC de pago informada por el banco.
    /// </summary>
    public DateTime? PaidAtUtc { get; private set; }

    /// <summary>
    /// Estado actual del QR.
    /// </summary>
    public PagoQrStatus Status { get; private set; }

    private PagoQr()
    {
    }

    /// <summary>
    /// Crea un QR pendiente después de que Banco Económico lo emitió correctamente.
    /// </summary>
    public PagoQr(
        string transactionId,
        string qrId,
        decimal amount,
        string currency,
        DateOnly dueDate,
        bool singleUse,
        bool modifyAmount,
        string? description,
        string? branchCode,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        TransactionId = transactionId;
        QrId = qrId;
        Amount = amount;
        Currency = currency;
        DueDate = dueDate;
        SingleUse = singleUse;
        ModifyAmount = modifyAmount;
        Description = description;
        BranchCode = branchCode;
        CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        Status = PagoQrStatus.Pendiente;
    }

    /// <summary>
    /// Marca el QR como pagado. La operación es idempotente para QR ya pagados.
    /// </summary>
    /// <param name="paidAtUtc">Fecha y hora UTC informada por el banco.</param>
    /// <returns><see langword="true"/> si se produjo la transición de estado.</returns>
    public bool MarkAsPaid(DateTime paidAtUtc)
    {
        if (Status == PagoQrStatus.Pagado)
        {
            return false;
        }

        Status = PagoQrStatus.Pagado;
        PaidAtUtc = DateTime.SpecifyKind(paidAtUtc, DateTimeKind.Utc);
        return true;
    }
}
