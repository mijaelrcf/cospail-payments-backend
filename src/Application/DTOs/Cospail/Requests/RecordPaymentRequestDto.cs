namespace Application.DTOs.Cospail.Requests;

/// <summary>
/// Solicitud para registrar un cobro en Cospail.
/// </summary>
public sealed class RecordPaymentRequestDto
{
    public int CreditNumber { get; set; }
    public int Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }

    /// <summary>
    /// Hora del pago en formato HH:mm:ss.
    /// </summary>
    public string PaymentTime { get; set; } = string.Empty;
}