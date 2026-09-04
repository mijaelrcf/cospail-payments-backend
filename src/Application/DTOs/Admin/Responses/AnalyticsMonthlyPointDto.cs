namespace Application.DTOs.Admin.Responses;

/// <summary>
/// Punto mensual de la serie del año en curso.
/// </summary>
public sealed class AnalyticsMonthlyPointDto
{
    public int Mes { get; init; }
    public int Ingresos { get; init; }
    public int QrGenerados { get; init; }
    public int Pagados { get; init; }
}
