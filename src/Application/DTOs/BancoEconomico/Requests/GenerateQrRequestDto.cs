namespace Application.DTOs.BancoEconomico.Requests;

/// <summary>
/// Request para generar el QR de cobro de un pago de deudas de Cospail.
/// El pago se crea previamente con InitiatePayment; el resto de los datos del
/// cobro (importe, moneda, vencimiento, transacción, etc.) se resuelven en la API.
/// </summary>
public sealed class GenerateQrRequestDto
{
    /// <summary>
    /// Identificador del pago agrupado de deudas de Cospail obtenido mediante InitiatePayment.
    /// </summary>
    public Guid PagoCospailId { get; set; }

    /// <summary>
    /// Código de sucursal comercial asociado al QR, si aplica (máximo 5 caracteres).
    /// </summary>
    public string? BranchCode { get; set; }
}
