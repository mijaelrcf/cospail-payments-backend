namespace Application.DTOs.Admin.Responses;

/// <summary>
/// Deuda individual incluida en un pago, para la vista del panel de administración.
/// </summary>
public sealed class AdminPaymentDebtDto
{
    public int CreditNumber { get; set; }
    public int Type { get; set; }
    public int NoticeNumber { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}
