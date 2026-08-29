namespace Application.DTOs.Admin.Responses;

/// <summary>
/// Pago de Cospail con sus deudas, para el listado del panel de administración.
/// </summary>
public sealed class AdminPaymentReportItemDto
{
    public Guid PagoCospailId { get; set; }
    public int FixedCode { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public string? MemberName { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public List<AdminPaymentDebtDto> Debts { get; set; } = [];
}
