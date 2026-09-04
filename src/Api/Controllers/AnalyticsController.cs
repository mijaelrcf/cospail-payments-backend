using Application.Interfaces.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

/// <summary>
/// Analíticas propias con contador puro de visitas (sin PII).
/// </summary>
[ApiController]
[Route("api/analytics")]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    /// <summary>
    /// Registra una visita del frontend cliente en el contador del día (Bolivia).
    /// El frontend debe llamarlo una sola vez por sesión (sessionStorage).
    /// </summary>
    [HttpPost("visits")]
    [AllowAnonymous]
    [EnableRateLimiting("AnalyticsVisits")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RegistrarVisita(CancellationToken cancellationToken)
    {
        await analyticsService.RegistrarVisitaAsync(cancellationToken);
        return NoContent();
    }
}

/// <summary>
/// Resumen de analíticas para el panel de administración (requiere rol Admin).
/// </summary>
[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AdminAnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    /// <summary>
    /// Devuelve ingresos, QR generados y pagados del día, mes y año en curso
    /// (Bolivia) más la serie mensual del año indicado.
    /// </summary>
    /// <param name="year">Año a consultar (opcional, por defecto el año en curso).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] int? year,
        CancellationToken cancellationToken
    )
    {
        if (year.HasValue && (year.Value < 2000 || year.Value > 2100))
        {
            return BadRequest("year debe estar entre 2000 y 2100.");
        }

        var result = await analyticsService.GetSummaryAsync(year, cancellationToken);
        return Ok(result);
    }
}
