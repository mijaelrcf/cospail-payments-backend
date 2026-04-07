namespace CospailPaymentApi.Application.DTOs.Cospail;

public sealed class DebtItemDto
{
    public int NoticeNumber { get; set; }
    public int CreditNumber { get; set; }
    public int Type { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}