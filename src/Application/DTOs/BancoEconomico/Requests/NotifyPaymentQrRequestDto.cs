using System.Text.Json.Serialization;

namespace Application.DTOs.BancoEconomico.Requests;

/// <summary>
/// Solicitud de notificación de pago QR enviada por Banco Económico.
/// </summary>
public sealed class NotifyPaymentQrRequestDto
{
    [JsonPropertyName("payment")]
    public PaymentDto? Payment { get; set; }

    /// <summary>
    /// Objeto PaymentQR enviado por Banco Económico al notificar un pago QR.
    /// </summary>
    public sealed class PaymentDto
    {
        [JsonPropertyName("qrId")]
        public string QrId { get; set; } = string.Empty;

        [JsonPropertyName("transactionId")]
        public string TransactionId { get; set; } = string.Empty;

        [JsonPropertyName("paymentDate")]
        public string PaymentDate { get; set; } = string.Empty;

        [JsonPropertyName("paymentTime")]
        public string PaymentTime { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("senderBankCode")]
        public string SenderBankCode { get; set; } = string.Empty;

        [JsonPropertyName("senderName")]
        public string SenderName { get; set; } = string.Empty;

        [JsonPropertyName("senderDocumentId")]
        public string SenderDocumentId { get; set; } = string.Empty;

        [JsonPropertyName("senderAccount")]
        public string SenderAccount { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("branchCode")]
        public string BranchCode { get; set; } = string.Empty;
    }
}
