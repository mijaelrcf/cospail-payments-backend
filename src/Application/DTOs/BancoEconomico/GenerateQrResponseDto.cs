namespace Application.DTOs.BancoEconomico;

/// <summary>
/// Respuesta de generación de QR del Banco Económico.
/// </summary>
public sealed class GenerateQrResponseDto
{
    public string? QrId { get; set; }
    public string? QrImage { get; set; }
    public int ResponseCode { get; set; }
    public string? Message { get; set; }
}