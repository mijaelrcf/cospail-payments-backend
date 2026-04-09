namespace Application.DTOs.Cospail.Requests;

/// <summary>
/// Solicitud para confirmar un pago y registrar el cobro en Cospail.
/// </summary>
public sealed class ConfirmPaymentRequestDto
{
    public int FixedCode { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public int CreditNumber { get; set; }
    public int Type { get; set; }
    public decimal Amount { get; set; }
}
