namespace Application.DTOs.BancoEconomico.Responses;

/// <summary>
/// Respuesta del listado de QR pagados en una fecha (7.6 paidQR).
/// </summary>
public sealed class PaidQrListResponseDto : IBanEcoResponse
{
    public List<PaymentQrDto> PaymentList { get; set; } = [];
    public int ResponseCode { get; set; }
    public string? Message { get; set; }
}
