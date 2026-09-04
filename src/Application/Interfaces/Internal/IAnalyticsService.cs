using Application.DTOs.Admin.Responses;

namespace Application.Interfaces.Internal;

/// <summary>
/// Analíticas propias con contador puro de visitas (sin PII).
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Registra una visita en el contador del día actual (Bolivia). Idempotencia
    /// por sesión la garantiza el frontend (1 beacon por sessionStorage).
    /// </summary>
    Task RegistrarVisitaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumen día/mes/año en curso (Bolivia) + serie mensual del año indicado.
    /// </summary>
    Task<AnalyticsSummaryResponseDto> GetSummaryAsync(
        int? year = null,
        CancellationToken cancellationToken = default
    );
}
