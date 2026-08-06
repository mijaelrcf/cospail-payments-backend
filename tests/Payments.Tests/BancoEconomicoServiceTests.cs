using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;
using Application.Interfaces.External;
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

    private static BancoEconomicoService CreateService(
        Mock<IBancoEconomicoQrClient> client,
        PaymentsDbContext db
    ) =>
        new(
            client.Object,
            db,
            new GenerateQrRequestDtoValidator(),
            new NotifyPaymentQrRequestDtoValidator(),
            NullLogger<BancoEconomicoService>.Instance
        );

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
}
