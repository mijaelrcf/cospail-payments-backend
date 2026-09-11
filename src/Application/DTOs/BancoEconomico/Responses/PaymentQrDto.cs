namespace Application.DTOs.BancoEconomico.Responses;

/// <summary>
/// Información de una transacción de pago QR (objeto PaymentQR del Anexo 1).
/// </summary>
public sealed class PaymentQrDto
{
    public string? QrId { get; set; }
    public string? TransactionId { get; set; }
    public string? PaymentDate { get; set; }
    public string? PaymentTime { get; set; }
    public string? Currency { get; set; }
    public decimal Amount { get; set; }
    public string? SenderBankCode { get; set; }
    public string? SenderName { get; set; }
    public string? SenderDocumentId { get; set; }
    public string? SenderAccount { get; set; }
    public string? Description { get; set; }
    public string? BranchCode { get; set; }
}
