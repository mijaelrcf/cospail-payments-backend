namespace Application.DTOs.BancoEconomico.Requests;

/// <summary>
/// Request para listar QR pagados en una fecha (7.6).
/// Fecha en formato yyyyMMdd. Se enlaza desde la ruta: GET paid-qr/{fecha}.
/// </summary>
public sealed class PaidQrListRequestDto
{
    public string Fecha { get; set; } = string.Empty;
}
