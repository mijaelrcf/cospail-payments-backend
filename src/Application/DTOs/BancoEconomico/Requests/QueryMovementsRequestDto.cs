namespace Application.DTOs.BancoEconomico.Requests;

/// <summary>
/// Request público para consultar movimientos (8.1).
/// La cuenta se resuelve en el servidor desde la configuración;
/// el cliente solo envía el período en formato yyyy-MM-dd.
/// </summary>
public sealed class QueryMovementsRequestDto
{
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
}
