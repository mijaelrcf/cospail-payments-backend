namespace Application.DTOs.BancoEconomico.Requests;

/// <summary>
/// Payload enviado a Banco Económico para anular un código QR
/// (<c>DELETE api/qrsimple/cancelQR</c>). Se construye internamente por la API.
/// </summary>
public sealed class AnnulQrBankRequestDto
{
    /// <summary>
    /// Identificador único del QR a anular.
    /// </summary>
    public string QrId { get; set; } = string.Empty;
}
