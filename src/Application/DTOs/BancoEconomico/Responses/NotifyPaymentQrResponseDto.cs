namespace Application.DTOs.BancoEconomico.Responses;

/// <summary>
/// Respuesta de acuse para la notificación de pago QR enviada por Banco Económico.
/// </summary>
public sealed class NotifyPaymentQrResponseDto
{
    public int ResponseCode { get; set; }

    public string Message { get; set; } = string.Empty;
}
