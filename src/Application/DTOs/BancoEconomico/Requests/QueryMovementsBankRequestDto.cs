namespace Application.DTOs.BancoEconomico.Requests;

/// <summary>
/// Payload enviado a Banco Económico para consultar movimientos (8.1 queryMovements).
/// Se construye internamente; el accountCode se toma de la configuración del servidor.
/// </summary>
public sealed class QueryMovementsBankRequestDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
}
