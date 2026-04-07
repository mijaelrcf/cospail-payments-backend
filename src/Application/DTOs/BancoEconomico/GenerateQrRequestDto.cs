namespace Application.DTOs.BancoEconomico;

/// <summary>
/// Request para generar un QR en Banco Económico.
/// </summary>
public sealed class GenerateQrRequestDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string AccountCredit { get; set; } = string.Empty;
    public string Currency { get; set; } = "BOB";
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string DueDate { get; set; } = string.Empty;
    public bool SingleUse { get; set; } = true;
    public bool ModifyAmount { get; set; } = false;
    public string? BranchCode { get; set; }
}