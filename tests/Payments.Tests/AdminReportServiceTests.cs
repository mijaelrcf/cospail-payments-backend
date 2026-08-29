using Application.DTOs.Admin.Requests;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Payments.Tests;

[TestClass]
public sealed class AdminReportServiceTests
{
    [TestClass]
    public sealed class GetPaymentReportAsyncTests
    {
        [TestMethod]
        public async Task GetPaymentReportAsync_WithNoStatus_ReturnsAllStatuses()
        {
            await using var db = CreateInMemoryDb();
            await SeedPaymentAsync(db, fixedCode: 123, status: PagoCospailStatus.CospailRegistrado);
            await SeedPaymentAsync(db, fixedCode: 321, status: PagoCospailStatus.Pendiente);
            var service = new AdminReportService(db);

            var result = await service.GetPaymentReportAsync(new AdminPaymentReportRequestDto());

            result.TotalCount.Should().Be(2);
            result.Items.Should().Contain(x => x.FixedCode == 123);
            result.Items.Should().Contain(x => x.FixedCode == 321);
        }

        [TestMethod]
        public async Task GetPaymentReportAsync_FiltersByStatus()
        {
            await using var db = CreateInMemoryDb();
            await SeedPaymentAsync(db, fixedCode: 123, status: PagoCospailStatus.CospailRegistrado);
            await SeedPaymentAsync(db, fixedCode: 321, status: PagoCospailStatus.Pendiente);
            var service = new AdminReportService(db);

            var result = await service.GetPaymentReportAsync(
                new AdminPaymentReportRequestDto
                {
                    Status = PagoCospailStatus.Pendiente
                }
            );

            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle(x => x.FixedCode == 321);
        }

        [TestMethod]
        public async Task GetPaymentReportAsync_FiltersByFixedCode()
        {
            await using var db = CreateInMemoryDb();
            await SeedPaymentAsync(db, fixedCode: 123, status: PagoCospailStatus.CospailRegistrado);
            await SeedPaymentAsync(db, fixedCode: 321, status: PagoCospailStatus.CospailRegistrado);
            var service = new AdminReportService(db);

            var result = await service.GetPaymentReportAsync(
                new AdminPaymentReportRequestDto
                {
                    Status = PagoCospailStatus.CospailRegistrado,
                    FixedCode = 123
                }
            );

            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle(x => x.FixedCode == 123);
        }

        [TestMethod]
        public async Task GetPaymentReportAsync_FiltersByDocumentIdPartial()
        {
            await using var db = CreateInMemoryDb();
            await SeedPaymentAsync(db, documentId: "CI1234567");
            await SeedPaymentAsync(db, documentId: "OTRO");
            var service = new AdminReportService(db);

            var result = await service.GetPaymentReportAsync(
                new AdminPaymentReportRequestDto { DocumentId = "12345" }
            );

            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle(x => x.DocumentId == "CI1234567");
        }

        [TestMethod]
        public async Task GetPaymentReportAsync_FiltersByDateRange()
        {
            await using var db = CreateInMemoryDb();
            await SeedPaymentAsync(db, createdAtUtc: new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc));
            await SeedPaymentAsync(db, createdAtUtc: new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc));
            var service = new AdminReportService(db);

            var result = await service.GetPaymentReportAsync(
                new AdminPaymentReportRequestDto
                {
                    From = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    To = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc)
                }
            );

            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle(x => x.CreatedAtUtc.Year == 2026 && x.CreatedAtUtc.Month == 1);
        }

        [TestMethod]
        public async Task GetPaymentReportAsync_PaginatesResults()
        {
            await using var db = CreateInMemoryDb();
            for (var i = 1; i <= 5; i++)
            {
                await SeedPaymentAsync(db, fixedCode: 100 + i);
            }
            var service = new AdminReportService(db);

            var result = await service.GetPaymentReportAsync(
                new AdminPaymentReportRequestDto { Page = 1, PageSize = 2 }
            );

            result.TotalCount.Should().Be(5);
            result.PageCount.Should().Be(3);
            result.Items.Count.Should().Be(2);
        }

        [TestMethod]
        public async Task GetPaymentReportAsync_IncludesNestedDebts()
        {
            await using var db = CreateInMemoryDb();
            var pago = await SeedPaymentAsync(db);
            var service = new AdminReportService(db);

            var result = await service.GetPaymentReportAsync(new AdminPaymentReportRequestDto());

            var item = result.Items.Should().ContainSingle(x => x.PagoCospailId == pago.Id).Subject;
            item.Debts.Should().ContainSingle();
            item.Debts[0].CreditNumber.Should().Be(5);
            item.Debts[0].Status.Should().Be("CospailRegistrado");
            item.TotalAmount.Should().Be(100.00m);
            item.Status.Should().Be("CospailRegistrado");
        }
    }

    [TestClass]
    public sealed class GetPaymentDetailAsyncTests
    {
        [TestMethod]
        public async Task GetPaymentDetailAsync_WhenExists_ReturnsDetailWithDebts()
        {
            await using var db = CreateInMemoryDb();
            var pago = await SeedPaymentAsync(db);
            var service = new AdminReportService(db);

            var result = await service.GetPaymentDetailAsync(pago.Id);

            result.Should().NotBeNull();
            result.FixedCode.Should().Be(123);
            result.Debts.Should().ContainSingle();
            result.MemberName.Should().Be("Juan Perez");
        }

        [TestMethod]
        public async Task GetPaymentDetailAsync_WhenNotExists_Throws()
        {
            await using var db = CreateInMemoryDb();
            var service = new AdminReportService(db);

            var act = () => service.GetPaymentDetailAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }

    private static PaymentsDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PaymentsDbContext(options);
    }

    private static async Task<PagoCospail> SeedPaymentAsync(
        PaymentsDbContext db,
        int fixedCode = 123,
        string documentId = "1234567",
        PagoCospailStatus status = PagoCospailStatus.CospailRegistrado,
        DateTime? createdAtUtc = null
    )
    {
        var pago = new PagoCospail(
            fixedCode,
            documentId,
            "Juan Perez",
            100.00m,
            createdAtUtc ?? new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)
        );
        pago.AddDeuda(
            new DeudaCospail(fixedCode, documentId, "Juan Perez", 5, 1, 5, 2026, 1, "2026-01", 100.00m)
        );

        db.PagosCospail.Add(pago);
        await db.SaveChangesAsync();

        if (status is PagoCospailStatus.QRGenerado)
        {
            var qr = new PagoQr(
                $"tx-{Guid.NewGuid():N}",
                $"qr-{Guid.NewGuid():N}",
                100.00m,
                "BOB",
                DateOnly.FromDateTime(DateTime.UtcNow),
                true,
                false,
                "5",
                "001",
                null,
                DateTime.UtcNow
            );
            db.PagosQr.Add(qr);
            await db.SaveChangesAsync();
            pago.MarkAsQrGenerated(qr.Id);
        }
        else if (status is PagoCospailStatus.Pagado or PagoCospailStatus.CospailRegistrado)
        {
            var qr = new PagoQr(
                $"tx-{Guid.NewGuid():N}",
                $"qr-{Guid.NewGuid():N}",
                100.00m,
                "BOB",
                DateOnly.FromDateTime(DateTime.UtcNow),
                true,
                false,
                "5",
                "001",
                null,
                DateTime.UtcNow
            );
            db.PagosQr.Add(qr);
            await db.SaveChangesAsync();
            pago.MarkAsQrGenerated(qr.Id);
            pago.MarkAsPagado();
            pago.Deudas.First().MarkAsPagado();
            if (status is PagoCospailStatus.CospailRegistrado)
            {
                pago.MarkAsCospailRegistrado();
                pago.Deudas.First().MarkAsCospailRegistrado();
            }
        }

        await db.SaveChangesAsync();
        return pago;
    }
}
