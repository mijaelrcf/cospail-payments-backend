namespace Domain.Entities;

/// <summary>
/// Representa una notificación de pago QR recibida de Banco Económico.
/// Almacena una instantánea de los datos del ordenante y de la transacción
/// reportados por el banco, para fines de trazabilidad y conciliación.
/// </summary>
public sealed class NotificacionPagoQr
{
    /// <summary>
    /// Identificador interno del registro.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Identificador del QR al que corresponde la notificación.
    /// </summary>
    public Guid PagoQrId { get; private set; }

    /// <summary>
    /// QR al que corresponde la notificación.
    /// </summary>
    public PagoQr Qr { get; private set; } = null!;

    /// <summary>
    /// Identificador único del QR emitido por Banco Económico.
    /// </summary>
    public string QrId { get; private set; } = string.Empty;

    /// <summary>
    /// Identificador de transacción único en el sistema consumidor.
    /// </summary>
    public string TransactionId { get; private set; } = string.Empty;

    /// <summary>
    /// Fecha de pago reportada por el banco (yyyy-MM-dd o yyyy-MM-ddTHH:mm:ss).
    /// </summary>
    public string PaymentDate { get; private set; } = string.Empty;

    /// <summary>
    /// Hora de pago reportada por el banco (HH:mm:ss).
    /// </summary>
    public string PaymentTime { get; private set; } = string.Empty;

    /// <summary>
    /// Fecha y hora UTC del pago, según la hora local informada por el banco.
    /// </summary>
    public DateTime PaymentAtUtc { get; private set; }

    /// <summary>
    /// Moneda del pago.
    /// </summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>
    /// Importe pagado reportado por el banco.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Código del banco emisor del ordenante.
    /// </summary>
    public string SenderBankCode { get; private set; } = string.Empty;

    /// <summary>
    /// Nombre del cliente/ordenante que realizó el pago.
    /// </summary>
    public string SenderName { get; private set; } = string.Empty;

    /// <summary>
    /// Documento de identidad del ordenante, cuando el banco lo reporta.
    /// </summary>
    public string SenderDocumentId { get; private set; } = string.Empty;

    /// <summary>
    /// Cuenta de origen del ordenante, con los últimos dígitos visibles.
    /// </summary>
    public string SenderAccount { get; private set; } = string.Empty;

    /// <summary>
    /// Descripción del cobro reportada por el banco.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Sucursal comercial asociada, si fue proporcionada.
    /// </summary>
    public string? BranchCode { get; private set; }

    /// <summary>
    /// Fecha y hora UTC en que se recibió la notificación.
    /// </summary>
    public DateTime ReceivedAtUtc { get; private set; }

    private NotificacionPagoQr() { }

    /// <summary>
    /// Registra la notificación de pago de un QR recibida de Banco Económico.
    /// </summary>
    public NotificacionPagoQr(
        PagoQr qr,
        string qrId,
        string transactionId,
        string paymentDate,
        string paymentTime,
        DateTime paymentAtUtc,
        string currency,
        decimal amount,
        string senderBankCode,
        string senderName,
        string senderDocumentId,
        string senderAccount,
        string description,
        string? branchCode,
        DateTime receivedAtUtc
    )
    {
        Id = Guid.NewGuid();
        Qr = qr;
        PagoQrId = qr.Id;
        QrId = qrId;
        TransactionId = transactionId;
        PaymentDate = paymentDate;
        PaymentTime = paymentTime;
        PaymentAtUtc = DateTime.SpecifyKind(paymentAtUtc, DateTimeKind.Utc);
        Currency = currency;
        Amount = amount;
        SenderBankCode = senderBankCode;
        SenderName = senderName;
        SenderDocumentId = senderDocumentId;
        SenderAccount = senderAccount;
        Description = description;
        BranchCode = branchCode;
        ReceivedAtUtc = DateTime.SpecifyKind(receivedAtUtc, DateTimeKind.Utc);
    }
}
