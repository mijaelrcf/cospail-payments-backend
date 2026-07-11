using System.Text.Json.Serialization;

namespace Application.DTOs.BancoEconomico.Responses;

/// <summary>
/// Respuesta de acuse para la notificación de pago QR enviada por Banco Económico.
/// </summary>
public sealed class NotifyPaymentQrResponseDto
{
    [JsonPropertyName("responseCode")]
    public int ResponseCode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
