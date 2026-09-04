namespace Application.DTOs.Admin.Responses;

/// <summary>
/// Resumen de analíticas del año en curso (fecha Bolivia) con aperturas
/// de día, mes y año más serie mensual.
/// </summary>
public sealed class AnalyticsSummaryResponseDto
{
    public DateOnly Fecha { get; init; }
    public int Anio { get; init; }
    public AnalyticsPeriodDto Dia { get; init; } = new();
    public AnalyticsPeriodDto Mes { get; init; } = new();
    public AnalyticsPeriodDto AnioResumen { get; init; } = new();
    public List<AnalyticsMonthlyPointDto> SerieMensual { get; init; } = [];
}
