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
    public async Task<AnalyticsSummaryResponseDto> GetSummaryAsync(
        int? year = null,
        CancellationToken cancellationToken = default
    )
    {
        var hoy = BoliviaTime.Today();
        var anio = year ?? hoy.Year;

        var dia = await GetPeriodAsync(hoy, hoy, cancellationToken);
        var mes = await GetPeriodAsync(
            new DateOnly(anio, hoy.Month, 1),
            new DateOnly(anio, hoy.Month, DateTime.DaysInMonth(anio, hoy.Month)),
            cancellationToken);
        // Si piden otro año, el "mes" se refiere al mismo mes de ese año.
        var anioPeriodo = await GetPeriodAsync(
            new DateOnly(anio, 1, 1),
            new DateOnly(anio, 12, 31),
            cancellationToken);

        var serie = new List<AnalyticsMonthlyPointDto>(capacity: 12);
        for (var m = 1; m <= 12; m++)
        {
            var desde = new DateOnly(anio, m, 1);
            var hasta = new DateOnly(anio, m, DateTime.DaysInMonth(anio, m));
            var p = await GetPeriodAsync(desde, hasta, cancellationToken);
            serie.Add(new AnalyticsMonthlyPointDto
            {
                Mes = m,
                Ingresos = p.Ingresos,
                QrGenerados = p.QrGenerados,
                Pagados = p.Pagados
            });
        }

        return new AnalyticsSummaryResponseDto
        {
            Fecha = hoy,
            Anio = anio,
            Dia = dia,
            Mes = mes,
            AnioResumen = anioPeriodo,
            SerieMensual = serie
        };
    }

    private async Task<AnalyticsPeriodDto> GetPeriodAsync(
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken
    )
    {
        var ingresos = await dbContext.ConteosVisitasDiario
            .Where(x => x.Fecha >= desde && x.Fecha <= hasta)
            .SumAsync(x => (int?)x.TotalVisitas, cancellationToken) ?? 0;

        var (desdeUtc, hastaUtcExclusivo) = RangoUtc(desde, hasta);

        var qrs = await dbContext.PagosQr
            .Where(x => x.CreatedAtUtc >= desdeUtc && x.CreatedAtUtc < hastaUtcExclusivo)
            .CountAsync(cancellationToken);

        var pagos = await dbContext.PagosQr
            .Where(x => x.Status == PagoQrStatus.Pagado
                && x.PaidAtUtc != null
                && x.PaidAtUtc >= desdeUtc
                && x.PaidAtUtc < hastaUtcExclusivo)
            .CountAsync(cancellationToken);

        return new AnalyticsPeriodDto
        {
            Ingresos = ingresos,
            QrGenerados = qrs,
            Pagados = pagos,
            ConversionQrAPago = qrs == 0 ? 0 : (double)pagos / qrs
        };
    }

    /// <summary>
    /// Convierte un rango de fechas Bolivia [desde, hasta] inclusivo a rango UTC
    /// [inicio, finExclusivo). Bolivia es UTC-04:00 fijo (sin DST):
    /// medianoche Bolivia = 04:00 UTC del mismo día.
    /// </summary>
    private static (DateTime DesdeUtc, DateTime HastaUtcExclusivo) RangoUtc(DateOnly desde, DateOnly hasta)
    {
        var desdeUtc = new DateTime(desde.Year, desde.Month, desde.Day, 4, 0, 0, DateTimeKind.Utc);
        var hastaUtcExclusivo = new DateTime(hasta.Year, hasta.Month, hasta.Day, 4, 0, 0, DateTimeKind.Utc).AddDays(1);
        return (desdeUtc, hastaUtcExclusivo);
    }
}
