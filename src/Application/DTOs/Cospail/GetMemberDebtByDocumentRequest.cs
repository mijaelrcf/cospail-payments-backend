namespace CospailPaymentApi.Application.DTOs.Cospail;

public sealed class GetMemberDebtByDocumentRequest
{
    public int FixedCode { get; set; }
    public string DocumentId { get; set; } = string.Empty;
}