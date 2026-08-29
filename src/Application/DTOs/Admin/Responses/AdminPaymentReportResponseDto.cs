namespace Application.DTOs.Admin.Responses;

/// <summary>
/// Resultado paginado del reporte de pagos de Cospail.
/// </summary>
public sealed class AdminPaymentReportResponseDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int PageCount { get; set; }
    public List<AdminPaymentReportItemDto> Items { get; set; } = [];
}
