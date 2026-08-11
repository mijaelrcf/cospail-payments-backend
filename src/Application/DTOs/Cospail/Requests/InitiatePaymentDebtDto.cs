namespace Application.DTOs.Cospail.Requests;

/// <summary>
/// Una deuda seleccionada por el socio para ser incluida en un pago.
/// </summary>
public sealed class InitiatePaymentDebtDto
{
    public int CreditNumber { get; set; }
    public int Type { get; set; }
    public decimal Amount { get; set; }
}