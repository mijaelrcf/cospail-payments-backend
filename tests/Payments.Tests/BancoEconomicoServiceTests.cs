using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;
using Application.DTOs.Cospail.Responses;
using Application.Interfaces.External;
using Application.Interfaces.Internal;
using Application.Interfaces.Persistence;
using Application.Services;
using Application.Validators;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Payments.Tests;

[TestClass]
public sealed class BancoEconomicoServiceTests
{
    [TestMethod]
    public async Task GenerateQrAsync_WhenBankSucceeds_PersistsPendingQr()
    {
        await using var db = CreateInMemoryDb();
        var service = CreateService(CreateClient("qr-001"), db);

        var result = await service.GenerateQrAsync(CreateGenerateRequest("tx-001"));

        result.QrId.Should().Be("qr-001");

        var savedQr = await db.PagosQr.SingleAsync(x => x.TransactionId == "tx-001");
        savedQr.Status.Should().Be(PagoQrStatus.Pendiente);
        savedQr.QrId.Should().Be("qr-001");
        savedQr.CreatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [TestMethod]
    public async Task GenerateQrAsync_WhenTransactionAlreadyExists_Throws()
    {
        await using var db = CreateInMemoryDb();
        db.PagosQr.Add(CreatePendingQr());
        await db.SaveChangesAsync();
        var service = CreateService(CreateClient("qr-001"), db);

        var act = () => service.GenerateQrAsync(CreateGenerateRequest("tx-001"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*transactionId*");
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenQrIsPending_MarksItPaidInUtc()
    {
        await using var db = CreateInMemoryDb();
        var qr = CreatePendingQr();
        db.PagosQr.Add(qr);
        await db.SaveChangesAsync();
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), db);

        var response = await service.HandlePaymentNotificationAsync(CreateNotification());

        response.ResponseCode.Should().Be(0);
        qr.Status.Should().Be(PagoQrStatus.Pagado);
        qr.PaidAtUtc.Should().Be(new DateTime(2026, 7, 14, 19, 0, 27, DateTimeKind.Utc));

        var stored = await db.PagosQr.SingleAsync(x => x.QrId == "qr-001");
        stored.Status.Should().Be(PagoQrStatus.Pagado);
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_PersistsNotificationWithSenderData()
    {
        await using var db = CreateInMemoryDb();
        var qr = CreatePendingQr();
        db.PagosQr.Add(qr);
        await db.SaveChangesAsync();
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), db);

        await service.HandlePaymentNotificationAsync(CreateNotification());

        var notification = db.NotificacionesPagoQr.Single();
        notification.PagoQrId.Should().Be(qr.Id);
        notification.QrId.Should().Be("qr-001");
        notification.TransactionId.Should().Be("tx-001");
        notification.Amount.Should().Be(35.50m);
        notification.Currency.Should().Be("BOB");
        notification.PaymentDate.Should().Be("2026-07-14");
        notification.PaymentTime.Should().Be("15:00:27");
        notification.PaymentAtUtc.Should().Be(new DateTime(2026, 7, 14, 19, 0, 27, DateTimeKind.Utc));
        notification.SenderBankCode.Should().Be("1016");
        notification.SenderName.Should().Be("Cliente");
        notification.SenderAccount.Should().Be("****1234");
        notification.Description.Should().Be("Pago");
        notification.BranchCode.Should().Be("001");
        notification.ReceivedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenRepeated_StoresAnotherNotification()
    {
        await using var db = CreateInMemoryDb();
        var qr = CreatePendingQr();
        qr.MarkAsPaid(DateTime.UtcNow);
        db.PagosQr.Add(qr);
        await db.SaveChangesAsync();
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), db);

        await service.HandlePaymentNotificationAsync(CreateNotification());
        await service.HandlePaymentNotificationAsync(CreateNotification());

        db.NotificacionesPagoQr.Should().HaveCount(2);
        db.PagosQr.Single(x => x.QrId == "qr-001").Status.Should().Be(PagoQrStatus.Pagado);
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenQrDoesNotExist_ReturnsValidationFailure()
    {
        await using var db = CreateInMemoryDb();
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), db);

        var act = () => service.HandlePaymentNotificationAsync(CreateNotification());

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*payment.qrId*");
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenTransactionDoesNotMatch_ReturnsValidationFailure()
    {
        await using var db = CreateInMemoryDb();
        db.PagosQr.Add(CreatePendingQr());
        await db.SaveChangesAsync();
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), db);

        var act = () => service.HandlePaymentNotificationAsync(CreateNotification("another-transaction"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*transactionId*");

        var stored = await db.PagosQr.SingleAsync(x => x.QrId == "qr-001");
        stored.Status.Should().Be(PagoQrStatus.Pendiente);
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenAlreadyPaid_IsIdempotent()
    {
        await using var db = CreateInMemoryDb();
        var qr = CreatePendingQr();
        qr.MarkAsPaid(DateTime.UtcNow);
        var originalPaidAt = qr.PaidAtUtc;
        db.PagosQr.Add(qr);
        await db.SaveChangesAsync();
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), db);

        var response = await service.HandlePaymentNotificationAsync(CreateNotification());

        response.ResponseCode.Should().Be(0);
        qr.PaidAtUtc.Should().Be(originalPaidAt);
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenAmountDoesNotMatchAndNotModifiable_Throws()
    {
        await using var db = CreateInMemoryDb();
        db.PagosQr.Add(CreatePendingQr());
        await db.SaveChangesAsync();
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), db);
        var notification = CreateNotification();
        notification.Payment!.Amount = 99.99m;

        var act = () => service.HandlePaymentNotificationAsync(notification);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*amount*");

        var stored = await db.PagosQr.SingleAsync(x => x.QrId == "qr-001");
        stored.Status.Should().Be(PagoQrStatus.Pendiente);
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenAmountDiffersButModifiable_Pays()
    {
        await using var db = CreateInMemoryDb();
        db.PagosQr.Add(new PagoQr(
            "tx-001", "qr-001", 35.50m, "BOB", new DateOnly(2026, 7, 31), true, true, "Pago", "001", DateTime.UtcNow));
        await db.SaveChangesAsync();
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), db);
        var notification = CreateNotification();
        notification.Payment!.Amount = 40.00m;

        var response = await service.HandlePaymentNotificationAsync(notification);

        response.ResponseCode.Should().Be(0);

        var stored = await db.PagosQr.SingleAsync(x => x.QrId == "qr-001");
        stored.Status.Should().Be(PagoQrStatus.Pagado);
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenCurrencyDoesNotMatch_Throws()
    {
        await using var db = CreateInMemoryDb();
        db.PagosQr.Add(CreatePendingQr());
        await db.SaveChangesAsync();
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), db);
        var notification = CreateNotification();
        notification.Payment!.Currency = "USD";

        var act = () => service.HandlePaymentNotificationAsync(notification);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*currency*");

        var stored = await db.PagosQr.SingleAsync(x => x.QrId == "qr-001");
        stored.Status.Should().Be(PagoQrStatus.Pendiente);
    }

    [TestMethod]
    public async Task GenerateQrAsync_WhenUniqueViolationOccurs_ThrowsArgumentException()
    {
        await using var db = CreateInMemoryDb();
        var service = CreateService(
            CreateClient("qr-001"),
            new ThrowingPaymentsDbContext(db)
        );

        var act = () => service.GenerateQrAsync(CreateGenerateRequest("tx-001"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*transactionId*");
    }

    [TestMethod]
    public async Task GenerateQrAsync_WhenPagoCospailLinked_ComputesTotalAndLinksQr()
    {
        await using var db = CreateInMemoryDb();
        var pagoCospail = await CreatePendingPaymentAsync(db);
        var service = CreateService(CreateClient("qr-001"), db);
        var request = CreateGenerateRequest("tx-001");
        request.PagoCospailId = pagoCospail.Id;
        request.Amount = 0m;

        var result = await service.GenerateQrAsync(request);

        result.QrId.Should().Be("qr-001");

        var savedQr = await db.PagosQr.SingleAsync(x => x.TransactionId == "tx-001");
        savedQr.Amount.Should().Be(100.00m);

        var updated = await db
            .PagosCospail.Include(x => x.Deudas)
            .SingleAsync(x => x.Id == pagoCospail.Id);
        updated.Status.Should().Be(PagoCospailStatus.QRGenerado);
        updated.PagoQrId.Should().Be(savedQr.Id);
    }

    [TestMethod]
    public async Task GenerateQrAsync_WhenPagoCospailDoesNotExist_Throws()
    {
        await using var db = CreateInMemoryDb();
        var service = CreateService(CreateClient("qr-001"), db);
        var request = CreateGenerateRequest("tx-001");
        request.PagoCospailId = Guid.NewGuid();

        var act = () => service.GenerateQrAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*pagoCospailId*");
    }

    [TestMethod]
    public async Task GenerateQrAsync_WhenPagoCospailAlreadyHasQr_Throws()
    {
        await using var db = CreateInMemoryDb();
        var pagoCospail = await CreatePendingPaymentAsync(db);
        pagoCospail.MarkAsQrGenerated(Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(CreateClient("qr-001"), db);
        var request = CreateGenerateRequest("tx-001");
        request.PagoCospailId = pagoCospail.Id;

        var act = () => service.GenerateQrAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*QR asociado*");
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenQrLinkedToPayment_RegistersDebtsInCospail()
    {
        await using var db = CreateInMemoryDb();
        var pagoCospail = await CreatePendingPaymentAsync(db);
        var qr = CreatePendingQr();
        db.PagosQr.Add(qr);
        await db.SaveChangesAsync();
        pagoCospail.MarkAsQrGenerated(qr.Id);
        await db.SaveChangesAsync();
        var cospailService = CreateCospailService(true);
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), db, cospailService);

        var response = await service.HandlePaymentNotificationAsync(CreateNotification());

        response.ResponseCode.Should().Be(0);

        var stored = await db
            .PagosCospail.Include(x => x.Deudas)
            .SingleAsync(x => x.Id == pagoCospail.Id);
        stored.Status.Should().Be(PagoCospailStatus.CospailRegistrado);
        stored.Deudas.Single().Status.Should().Be(DeudaCospailStatus.CospailRegistrado);

        cospailService.Verify(
            x => x.RecordDebtPaymentAsync(5, 1, 100.00m, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenCospailRegistrationFails_KeepsPaymentPagado()
    {
        await using var db = CreateInMemoryDb();
        var pagoCospail = await CreatePendingPaymentAsync(db);
        var qr = CreatePendingQr();
        db.PagosQr.Add(qr);
        await db.SaveChangesAsync();
        pagoCospail.MarkAsQrGenerated(qr.Id);
        await db.SaveChangesAsync();
        var cospailService = CreateCospailService(false);
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), db, cospailService);

        var response = await service.HandlePaymentNotificationAsync(CreateNotification());

        response.ResponseCode.Should().Be(0);

        var stored = await db
            .PagosCospail.Include(x => x.Deudas)
            .SingleAsync(x => x.Id == pagoCospail.Id);
        stored.Status.Should().Be(PagoCospailStatus.Pagado);
        stored.Deudas.Single().Status.Should().Be(DeudaCospailStatus.Pagado);

        cospailService.Verify(
            x => x.RecordDebtPaymentAsync(5, 1, 100.00m, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenPaymentAlreadyRegistered_IsIdempotent()
    {
        await using var db = CreateInMemoryDb();
        var pagoCospail = await CreatePendingPaymentAsync(db);
        var qr = CreatePendingQr();
        db.PagosQr.Add(qr);
        await db.SaveChangesAsync();
        pagoCospail.MarkAsQrGenerated(qr.Id);
        await db.SaveChangesAsync();
        var cospailService = CreateCospailService(true);
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), db, cospailService);

        await service.HandlePaymentNotificationAsync(CreateNotification());
        await service.HandlePaymentNotificationAsync(CreateNotification());

        cospailService.Verify(
            x => x.RecordDebtPaymentAsync(5, 1, 100.00m, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    private static BancoEconomicoService CreateService(
        Mock<IBancoEconomicoQrClient> client,
        IPaymentsDbContext db,
        Mock<ICospailService>? cospailService = null
    ) =>
        new(
            client.Object,
            db,
            new GenerateQrRequestDtoValidator(),
            new NotifyPaymentQrRequestDtoValidator(),
            (cospailService ?? CreateCospailService()).Object,
            NullLogger<BancoEconomicoService>.Instance
        );

    private static Mock<ICospailService> CreateCospailService(bool recordDebtSuccess = true)
    {
        var mock = new Mock<ICospailService>();
        mock.Setup(x => x.RecordDebtPaymentAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecordPaymentResponseDto
            {
                Success = recordDebtSuccess,
                Message = recordDebtSuccess ? "Cobro registrado correctamente." : "Error en Cospail"
            });
        return mock;
    }

    private static async Task<PagoCospail> CreatePendingPaymentAsync(PaymentsDbContext db)
    {
        var pagoCospail = new PagoCospail(123, "1234567", "Juan Perez", 100.00m, DateTime.UtcNow);
        pagoCospail.AddDeuda(
            new DeudaCospail(123, "1234567", "Juan Perez", 5, 1, 5, 2026, 7, "2026-07", 100.00m)
        );
        db.PagosCospail.Add(pagoCospail);
        await db.SaveChangesAsync();
        return pagoCospail;
    }

    private static PaymentsDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PaymentsDbContext(options);
    }

    private static Mock<IBancoEconomicoQrClient> CreateClient(string qrId)
    {
        var client = new Mock<IBancoEconomicoQrClient>();
        client.Setup(x => x.AuthenticateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateResponseDto { Token = "token", ResponseCode = 0 });
        client.Setup(x => x.GenerateQrAsync("token", It.IsAny<GenerateQrRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenerateQrResponseDto { QrId = qrId, ResponseCode = 0 });
        return client;
    }

    private static GenerateQrRequestDto CreateGenerateRequest(string transactionId) => new()
    {
        TransactionId = transactionId,
        Currency = "BOB",
        Amount = 35.50m,
        DueDate = "2026-07-31",
        SingleUse = true,
        BranchCode = "001"
    };

    private static PagoQr CreatePendingQr() => new(
        "tx-001", "qr-001", 35.50m, "BOB", new DateOnly(2026, 7, 31), true, false, "Pago", "001", DateTime.UtcNow);

    private static NotifyPaymentQrRequestDto CreateNotification(string transactionId = "tx-001") => new()
    {
        Payment = new NotifyPaymentQrRequestDto.PaymentDto
        {
            QrId = "qr-001", TransactionId = transactionId, PaymentDate = "2026-07-14", PaymentTime = "15:00:27",
            Currency = "BOB", Amount = 35.50m, SenderBankCode = "1016", SenderName = "Cliente", SenderAccount = "****1234",
            Description = "Pago", BranchCode = "001"
        }
    };

    private sealed class ThrowingPaymentsDbContext(IPaymentsDbContext inner) : IPaymentsDbContext
    {
        public DbSet<PagoQr> PagosQr => inner.PagosQr;

        public DbSet<PagoCospail> PagosCospail => inner.PagosCospail;

        public DbSet<DeudaCospail> DeudasCospail => inner.DeudasCospail;

        public DbSet<NotificacionPagoQr> NotificacionesPagoQr => inner.NotificacionesPagoQr;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new DbUpdateException(
                "An error occurred while saving the entity changes.",
                new InvalidOperationException("duplicate key value violates unique constraint (23505)")
            );
    }
}

