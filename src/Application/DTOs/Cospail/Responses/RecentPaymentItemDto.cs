namespace Application.DTOs.Cospail.Responses;

/// <summary>
/// Resumen de un pago reciente de Cospail con sus deudas para vista maestro-detalle.
/// </summary>
public sealed class RecentPaymentItemDto
{
    public Guid PagoCospailId { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PaymentDebtDetailDto> Debts { get; set; } = new();
}
