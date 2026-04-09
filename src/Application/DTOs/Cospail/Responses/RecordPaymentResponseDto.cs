namespace Application.DTOs.Cospail.Responses;

/// <summary>
/// Respuesta del registro de cobro en Cospail.
/// </summary>
public sealed class RecordPaymentResponseDto
{
    public bool Success { get; set; }
    public string RawResult { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}