namespace Application.DTOs.Cospail.Responses;


/// <summary>
/// Respuesta de confirmación de pago.
/// </summary>
public sealed class ConfirmPaymentResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public int FixedCode { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public int CreditNumber { get; set; }
    public int Type { get; set; }
    public decimal Amount { get; set; }

    public string? MemberName { get; set; }
}