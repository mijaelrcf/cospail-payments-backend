namespace CospailPaymentApi.Application.DTOs.Cospail;

public sealed class GetMemberDebtByDocumentResponse
{
    public int FixedCode { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public string? MemberName { get; set; }
    public MemberDebtStatus Status { get; set; }
    public List<DebtItemDto> Debts { get; set; } = new();
}