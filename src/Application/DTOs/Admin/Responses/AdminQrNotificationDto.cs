namespace Application.DTOs.Admin.Responses;

/// <summary>
/// Notificación de pago QR de Banco Económico, para conciliación en el detalle.
/// </summary>
public sealed class AdminQrNotificationDto
{
    public string QrId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string? PaymentDate { get; set; }
    public string? PaymentTime { get; set; }
    public DateTime? PaymentAtUtc { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? SenderBankCode { get; set; }
    public string? SenderName { get; set; }
    public string? SenderDocumentId { get; set; }
    public string? SenderAccount { get; set; }
    public string? Description { get; set; }
    public string? BranchCode { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
}
