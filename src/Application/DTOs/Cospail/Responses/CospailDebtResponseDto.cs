namespace Application.DTOs.Cospail.Responses;

/// <summary>
/// Representa la deuda obtenida desde el servicio SOAP de Cospail.
/// </summary>
public sealed class CospailDebtResponseDto
{
    public int FixedCode { get; set; }
    public int? NoticeNumber { get; set; }
    public int? CreditNumber { get; set; }
    public int? Type { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}
