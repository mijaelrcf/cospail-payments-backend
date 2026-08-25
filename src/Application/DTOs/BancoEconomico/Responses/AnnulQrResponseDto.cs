namespace Application.DTOs.BancoEconomico.Responses;

/// <summary>
/// Respuesta de anulación de QR del Banco Económico.
/// </summary>
public sealed class AnnulQrResponseDto
{
    public int ResponseCode { get; set; }
    public string? Message { get; set; }
}
