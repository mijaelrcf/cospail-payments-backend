using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;
using Application.Interfaces.External;
using Application.Interfaces.Persistence;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
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
        var client = CreateClient("qr-001");
        var repository = new Mock<IPagoQrRepository>();
        PagoQr? savedQr = null;
        repository.Setup(x => x.GetByTransactionIdAsync("tx-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagoQr?)null);
        repository.Setup(x => x.AddAsync(It.IsAny<PagoQr>(), It.IsAny<CancellationToken>()))
            .Callback<PagoQr, CancellationToken>((qr, _) => savedQr = qr)
            .Returns(Task.CompletedTask);
        var service = CreateService(client, repository);

        var result = await service.GenerateQrAsync(CreateGenerateRequest("tx-001"));

        result.QrId.Should().Be("qr-001");
        savedQr.Should().NotBeNull();
        savedQr!.Status.Should().Be(PagoQrStatus.Pendiente);
        savedQr.TransactionId.Should().Be("tx-001");
        savedQr.QrId.Should().Be("qr-001");
        savedQr.CreatedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [TestMethod]
    public async Task GenerateQrAsync_WhenPersistenceFails_PropagatesFailure()
    {
        var client = CreateClient("qr-001");
        var repository = new Mock<IPagoQrRepository>();
        repository.Setup(x => x.GetByTransactionIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagoQr?)null);
        repository.Setup(x => x.AddAsync(It.IsAny<PagoQr>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));
        var service = CreateService(client, repository);

        var act = () => service.GenerateQrAsync(CreateGenerateRequest("tx-001"));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database unavailable");
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenQrIsPending_MarksItPaidInUtc()
    {
        var qr = CreatePendingQr();
        var repository = new Mock<IPagoQrRepository>();
        repository.Setup(x => x.GetByQrIdAsync("qr-001", It.IsAny<CancellationToken>())).ReturnsAsync(qr);
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), repository);

        var response = await service.HandlePaymentNotificationAsync(CreateNotification());

        response.ResponseCode.Should().Be(0);
        qr.Status.Should().Be(PagoQrStatus.Pagado);
        qr.PaidAtUtc.Should().Be(new DateTime(2026, 7, 14, 19, 0, 27, DateTimeKind.Utc));
        repository.Verify(x => x.UpdateAsync(qr, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenQrDoesNotExist_ReturnsValidationFailure()
    {
        var repository = new Mock<IPagoQrRepository>();
        repository.Setup(x => x.GetByQrIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagoQr?)null);
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), repository);

        var act = () => service.HandlePaymentNotificationAsync(CreateNotification());

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*payment.qrId*");
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenTransactionDoesNotMatch_ReturnsValidationFailure()
    {
        var repository = new Mock<IPagoQrRepository>();
        repository.Setup(x => x.GetByQrIdAsync("qr-001", It.IsAny<CancellationToken>())).ReturnsAsync(CreatePendingQr());
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), repository);

        var act = () => service.HandlePaymentNotificationAsync(CreateNotification("another-transaction"));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*transactionId*");
        repository.Verify(x => x.UpdateAsync(It.IsAny<PagoQr>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task HandlePaymentNotificationAsync_WhenAlreadyPaid_IsIdempotent()
    {
        var qr = CreatePendingQr();
        qr.MarkAsPaid(DateTime.UtcNow);
        var originalPaidAt = qr.PaidAtUtc;
        var repository = new Mock<IPagoQrRepository>();
        repository.Setup(x => x.GetByQrIdAsync("qr-001", It.IsAny<CancellationToken>())).ReturnsAsync(qr);
        var service = CreateService(new Mock<IBancoEconomicoQrClient>(), repository);

        var response = await service.HandlePaymentNotificationAsync(CreateNotification());

        response.ResponseCode.Should().Be(0);
        qr.PaidAtUtc.Should().Be(originalPaidAt);
        repository.Verify(x => x.UpdateAsync(It.IsAny<PagoQr>(), It.IsAny<CancellationToken>()), Times.Never);
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

    private static BancoEconomicoService CreateService(Mock<IBancoEconomicoQrClient> client, Mock<IPagoQrRepository> repository) =>
        new(client.Object, repository.Object, NullLogger<BancoEconomicoService>.Instance);

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
