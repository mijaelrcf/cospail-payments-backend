using Application.DTOs.Admin.Responses;
using Application.Interfaces.Internal;
using Application.Interfaces.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

/// <summary>
/// Analíticas con contador puro de visitas por día (Bolivia).
/// QR generados y pagados se derivan de <see cref="PagoQr"/>.
/// </summary>
public sealed class AnalyticsService(IPaymentsDbContext dbContext) : IAnalyticsService
{
    /// <inheritdoc />
    public async Task RegistrarVisitaAsync(CancellationToken cancellationToken = default)
    {
        var hoy = BoliviaTime.Today();

        var conteo = await dbContext.ConteosVisitasDiario
            .SingleOrDefaultAsync(x => x.Fecha == hoy, cancellationToken);

        if (conteo is null)
        {
            conteo = new ConteoVisitasDiario(hoy);
            conteo.Increment();
            dbContext.ConteosVisitasDiario.Add(conteo);
        }
        else
        {
            conteo.Increment();
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Carrera: dos hilos crearon la fila del día a la vez. Reintenta el incremento.
            var existente = await dbContext.ConteosVisitasDiario
                .SingleOrDefaultAsync(x => x.Fecha == hoy, cancellationToken);
            if (existente is null)
            {
                throw;
            }

            existente.Increment();
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Optimizado para ~15 roundtrips: las visitas del año se agrupan en una sola
    /// query y los dos conteos de QR por período van en una sola query con conteo
    /// condicional. Mes y año se derivan de la serie mensual (mismos numeradores
    /// y denominadores, números idénticos).
    /// </remarks>
    public async Task<AnalyticsSummaryResponseDto> GetSummaryAsync(
        int? year = null,
        CancellationToken cancellationToken = default
    )
    {
        var hoy = BoliviaTime.Today();
        var anio = year ?? hoy.Year;

        var visitasAnio = await GetVisitasAnioAsync(anio, cancellationToken);

        var dia = await GetPeriodAsync(hoy, hoy, await GetVisitasDiaAsync(hoy, cancellationToken), cancellationToken);

        var serie = new List<AnalyticsMonthlyPointDto>(capacity: 12);
        for (var m = 1; m <= 12; m++)
        {
            var (desde, hasta) = MonthBounds(anio, m);
            var (qrs, pagos) = await GetQrCountsAsync(desde, hasta, cancellationToken);
            serie.Add(new AnalyticsMonthlyPointDto
            {
                Mes = m,
                Ingresos = SumVisitas(visitasAnio, desde, hasta),
                QrGenerados = qrs,
                Pagados = pagos
            });
        }

        // Si piden otro año, el "mes" se refiere al mismo mes de ese año.
        var puntoMes = serie[hoy.Month - 1];

        return new AnalyticsSummaryResponseDto
        {
            Fecha = hoy,
            Anio = anio,
            Dia = dia,
            Mes = ToPeriod(puntoMes.Ingresos, puntoMes.QrGenerados, puntoMes.Pagados),
            AnioResumen = ToPeriod(
                serie.Sum(x => x.Ingresos),
                serie.Sum(x => x.QrGenerados),
                serie.Sum(x => x.Pagados)),
            SerieMensual = serie
        };
    }

    private static (DateOnly Desde, DateOnly Hasta) MonthBounds(int anio, int mes)
    {
        var desde = new DateOnly(anio, mes, 1);
        return (desde, new DateOnly(anio, mes, DateTime.DaysInMonth(anio, mes)));
    }

    /// <summary>
    /// Visitas del año agrupadas por fecha en una sola query.
    /// La columna ya es fecha calendario de Bolivia, sin conversión de zona.
    /// </summary>
    private async Task<Dictionary<DateOnly, int>> GetVisitasAnioAsync(
        int anio,
        CancellationToken cancellationToken
    )
    {
        var desde = new DateOnly(anio, 1, 1);
        var hasta = new DateOnly(anio, 12, 31);

        return await dbContext.ConteosVisitasDiario
            .Where(x => x.Fecha >= desde && x.Fecha <= hasta)
            .GroupBy(x => x.Fecha)
            .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.TotalVisitas) })
            .ToDictionaryAsync(x => x.Fecha, x => x.Total, cancellationToken);
    }

    /// <summary>
    /// Visitas de un día concreto. Va en query propia porque el día siempre es
    /// "hoy" aunque se consulte la serie de otro año.
    /// </summary>
    private async Task<int> GetVisitasDiaAsync(DateOnly hoy, CancellationToken cancellationToken) =>
        await dbContext.ConteosVisitasDiario
            .Where(x => x.Fecha == hoy)
            .Select(x => (int?)x.TotalVisitas)
            .SingleOrDefaultAsync(cancellationToken) ?? 0;

    private static int SumVisitas(
        Dictionary<DateOnly, int> visitasAnio,
        DateOnly desde,
        DateOnly hasta
    )
    {
        var total = 0;
        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
        {
            total += visitasAnio.GetValueOrDefault(fecha);
        }

        return total;
    }

    private async Task<AnalyticsPeriodDto> GetPeriodAsync(
        DateOnly desde,
        DateOnly hasta,
        int ingresos,
        CancellationToken cancellationToken
    )
    {
        var (qrs, pagos) = await GetQrCountsAsync(desde, hasta, cancellationToken);

        return ToPeriod(ingresos, qrs, pagos);
    }

    /// <summary>
    /// QR generados y pagados de un período en una sola query con conteo condicional.
    /// Predicados idénticos a las dos queries originales: los generados filtran por
    /// creación y los pagados solo por fecha de pago (sin filtrar por creación).
    /// </summary>
    private async Task<(int Qrs, int Pagos)> GetQrCountsAsync(
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken
    )
    {
        var (desdeUtc, hastaUtcExclusivo) = BoliviaTime.ToUtcRange(desde, hasta);

        var counts = await dbContext.PagosQr
            .Where(x => (x.CreatedAtUtc >= desdeUtc && x.CreatedAtUtc < hastaUtcExclusivo)
                || (x.Status == PagoQrStatus.Pagado
                    && x.PaidAtUtc != null
                    && x.PaidAtUtc >= desdeUtc
                    && x.PaidAtUtc < hastaUtcExclusivo))
            .GroupBy(x => 1)
            .Select(g => new
            {
                Qrs = g.Count(x => x.CreatedAtUtc >= desdeUtc && x.CreatedAtUtc < hastaUtcExclusivo),
                Pagos = g.Count(x => x.Status == PagoQrStatus.Pagado
                    && x.PaidAtUtc != null
                    && x.PaidAtUtc >= desdeUtc
                    && x.PaidAtUtc < hastaUtcExclusivo)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return counts is null ? (0, 0) : (counts.Qrs, counts.Pagos);
    }

    private static AnalyticsPeriodDto ToPeriod(int ingresos, int qrs, int pagos) =>
        new()
        {
            Ingresos = ingresos,
            QrGenerados = qrs,
            Pagados = pagos,
            ConversionQrAPago = qrs == 0 ? 0 : (double)pagos / qrs
        };
}
