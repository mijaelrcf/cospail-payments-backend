namespace Application.DTOs.Cospail.Responses;

/// <summary>
/// Detalle de una deuda dentro de un pago de Cospail.
/// </summary>
public sealed class PaymentDebtDetailDto
{
    public int CreditNumber { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
