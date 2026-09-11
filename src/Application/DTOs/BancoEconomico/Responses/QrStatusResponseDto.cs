namespace Application.DTOs.BancoEconomico.Responses;

/// <summary>
/// Respuesta de verificación de estado de QR (7.4 statusQR).
/// statusQrCode: 0 = activo pendiente, 1 = pagado, 9 = anulado.
/// </summary>
public sealed class QrStatusResponseDto
{
    public int? StatusQrCode { get; set; }
    public List<PaymentQrDto> Payment { get; set; } = [];
    public int ResponseCode { get; set; }
    public string? Message { get; set; }
}
