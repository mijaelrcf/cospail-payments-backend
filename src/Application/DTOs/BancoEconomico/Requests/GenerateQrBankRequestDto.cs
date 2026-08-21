namespace Application.DTOs.BancoEconomico.Requests;

/// <summary>
/// Payload enviado a Banco Económico para generar un código QR.
/// Se construye internamente por la API a partir del pago de Cospail;
/// nunca se recibe directamente del cliente.
/// </summary>
public sealed class GenerateQrBankRequestDto
{
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Cuenta de acreditación; el cliente HTTP la reemplaza por el valor configurado en el servidor.
    /// </summary>
    public string AccountCredit { get; set; } = string.Empty;

    public string Currency { get; set; } = "BOB";
    public decimal Amount { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Fecha de vencimiento del QR con formato yyyy-MM-dd.
    /// </summary>
    public string DueDate { get; set; } = string.Empty;
    public bool SingleUse { get; set; } = true;
    public bool ModifyAmount { get; set; } = false;
    public string? BranchCode { get; set; }
}
