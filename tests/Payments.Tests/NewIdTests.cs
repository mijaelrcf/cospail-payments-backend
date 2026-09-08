using Domain.Common;
using Domain.Entities;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Payments.Tests;

[TestClass]
public sealed class NewIdTests
{
    [TestMethod]
    public void V7_GeneratesVersion7Uuid()
    {
        var id = NewId.V7();

        id.Version.Should().Be(7);
    }

    [TestMethod]
    public void Entities_GenerateVersion7Ids()
    {
        var pagoQr = new PagoQr(
            "tx-001", "qr-001", 10.00m, "BOB", new DateOnly(2026, 9, 7),
            true, false, null, null, null, DateTime.UtcNow);

        var pagoCospail = new PagoCospail(123, "1234567", "Juan Perez", 10.00m, DateTime.UtcNow);

        var deuda = new DeudaCospail(123, "1234567", "Juan Perez", 5, 1, 5, 2026, 9, "2026-09", 10.00m);

        var notificacion = new NotificacionPagoQr(
            pagoQr, "qr-001", "tx-001", "2026-09-07", "15:13:57", DateTime.UtcNow,
            "BOB", 10.00m, "1016", "Cliente", "0", "****0182", "3531613", null, DateTime.UtcNow);

        pagoQr.Id.Version.Should().Be(7);
        pagoCospail.Id.Version.Should().Be(7);
        deuda.Id.Version.Should().Be(7);
        notificacion.Id.Version.Should().Be(7);
    }
}
