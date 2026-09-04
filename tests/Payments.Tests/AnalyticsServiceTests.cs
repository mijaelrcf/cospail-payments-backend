using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Payments.Tests;

[TestClass]
public sealed class AnalyticsServiceTests
{
    [TestMethod]
    public async Task RegistrarVisitaAsync_CreaFilaDelDiaEIncrementa()
    {
        await using var db = CreateInMemoryDb();
        var service = new AnalyticsService(db);

        await service.RegistrarVisitaAsync();
        await service.RegistrarVisitaAsync();
        await service.RegistrarVisitaAsync();

        var hoy = HoyBolivia();
        var conteo = await db.ConteosVisitasDiario.SingleAsync(x => x.Fecha == hoy);
        conteo.TotalVisitas.Should().Be(3);
    }

    [TestMethod]
    public async Task GetSummaryAsync_CuentaIngresosQrYPagosDelDia()
    {
        await using var db = CreateInMemoryDb();
        var service = new AnalyticsService(db);

        await service.RegistrarVisitaAsync();
        await service.RegistrarVisitaAsync();

        var ahora = DateTime.UtcNow;
        db.PagosQr.Add(CrearQr($"tx-{Guid.NewGuid():N}", $"qr-{Guid.NewGuid():N}", ahora, pagado: false));
        db.PagosQr.Add(CrearQr($"tx-{Guid.NewGuid():N}", $"qr-{Guid.NewGuid():N}", ahora, pagado: true));
        await db.SaveChangesAsync();

        var resumen = await service.GetSummaryAsync();

        resumen.Dia.Ingresos.Should().Be(2);
        resumen.Dia.QrGenerados.Should().Be(2);
        resumen.Dia.Pagados.Should().Be(1);
        resumen.Dia.ConversionQrAPago.Should().BeApproximately(0.5, 0.0001);
        resumen.Mes.Ingresos.Should().BeGreaterThanOrEqualTo(2);
        resumen.AnioResumen.Ingresos.Should().BeGreaterThanOrEqualTo(2);
        resumen.SerieMensual.Should().HaveCount(12);
        resumen.SerieMensual.Sum(x => x.Ingresos).Should().BeGreaterThanOrEqualTo(2);
    }

    [TestMethod]
    public async Task GetSummaryAsync_SinDatos_RetornaCeros()
    {
        await using var db = CreateInMemoryDb();
        var service = new AnalyticsService(db);

        var resumen = await service.GetSummaryAsync();

        resumen.Dia.Ingresos.Should().Be(0);
        resumen.Dia.QrGenerados.Should().Be(0);
        resumen.Dia.Pagados.Should().Be(0);
        resumen.Dia.ConversionQrAPago.Should().Be(0);
    }

    private static PaymentsDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PaymentsDbContext(options);
    }

    private static DateOnly HoyBolivia()
    {
        var utc = DateTime.UtcNow;
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");
        }
        catch
        {
            tz = TimeZoneInfo.CreateCustomTimeZone("Bolivia", TimeSpan.FromHours(-4), "Bolivia", "Bolivia");
        }

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, tz));
    }

    private static PagoQr CrearQr(string transactionId, string qrId, DateTime ahora, bool pagado)
    {
        var qr = new PagoQr(
            transactionId,
            qrId,
            100.00m,
            "BOB",
            DateOnly.FromDateTime(ahora),
            true,
            false,
            "5",
            "001",
            null,
            ahora);
        if (pagado)
        {
            qr.MarkAsPaid(ahora);
        }

        return qr;
    }
}
