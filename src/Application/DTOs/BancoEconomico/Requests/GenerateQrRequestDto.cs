namespace Application.DTOs.BancoEconomico.Requests;

/// <summary>
/// Request para generar un QR en Banco Económico.
/// </summary>
public sealed class GenerateQrRequestDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string AccountCredit { get; set; } = string.Empty;
    public string Currency { get; set; } = "BOB";

    /// <summary>
    /// Importe del QR. Cuando se indica <see cref="PagoCospailId"/> el importe se
    /// calcula desde el total de las deudas del pago y este valor se ignora.
    /// </summary>
    public decimal Amount { get; set; }

    public string? Description { get; set; }
    public string DueDate { get; set; } = string.Empty;
    public bool SingleUse { get; set; } = true;
    public bool ModifyAmount { get; set; } = false;
    public string? BranchCode { get; set; }

    /// <summary>
    /// Identificador del pago agrupado de deudas de Cospail al que se asocia el QR.
    /// Opcional: su ausencia genera un QR independiente con el importe indicado.
    /// </summary>
    public Guid? PagoCospailId { get; set; }
}