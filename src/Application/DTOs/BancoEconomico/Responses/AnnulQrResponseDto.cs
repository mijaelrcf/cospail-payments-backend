namespace Application.DTOs.BancoEconomico.Responses;

/// <summary>
/// Respuesta de anulación de QR del Banco Económico.
/// </summary>
public sealed class AnnulQrResponseDto : IBanEcoResponse
{
    public int ResponseCode { get; set; }
    public string? Message { get; set; }
}
