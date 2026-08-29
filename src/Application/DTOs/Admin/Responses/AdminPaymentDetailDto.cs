namespace Application.DTOs.Admin.Responses;

/// <summary>
/// Detalle de un pago de Cospail, sus deudas y la información de pago QR
/// asociada (para conciliación contra Banco Económico).
/// </summary>
public sealed class AdminPaymentDetailDto
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
    public AdminQrNotificationDto? QrNotification { get; set; }
}
