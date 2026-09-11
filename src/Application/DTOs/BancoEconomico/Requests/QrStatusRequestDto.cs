namespace Application.DTOs.BancoEconomico.Requests;

/// <summary>
/// Request para verificar el estado de un QR (7.4).
/// Se enlaza desde la ruta: GET qr-status/{qrId}.
/// </summary>
public sealed class QrStatusRequestDto
{
    public string QrId { get; set; } = string.Empty;
}
