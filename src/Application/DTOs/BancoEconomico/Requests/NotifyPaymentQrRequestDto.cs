namespace Application.DTOs.BancoEconomico.Requests;

/// <summary>
/// Solicitud de notificación de pago QR enviada por Banco Económico.
/// </summary>
public sealed class NotifyPaymentQrRequestDto
{
    public PaymentDto? Payment { get; set; }

    /// <summary>
    /// Objeto PaymentQR enviado por Banco Económico al notificar un pago QR.
    /// </summary>
    public sealed class PaymentDto
    {
        public string QrId { get; set; } = string.Empty;

        public string TransactionId { get; set; } = string.Empty;

        public string PaymentDate { get; set; } = string.Empty;

        public string PaymentTime { get; set; } = string.Empty;

        public string Currency { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string SenderBankCode { get; set; } = string.Empty;

        public string SenderName { get; set; } = string.Empty;

        public string SenderDocumentId { get; set; } = string.Empty;

        public string SenderAccount { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string BranchCode { get; set; } = string.Empty;
    }
}
