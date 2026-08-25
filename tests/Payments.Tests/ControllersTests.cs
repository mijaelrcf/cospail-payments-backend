using Api.Controllers;
using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;
using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;
using Application.Interfaces.Internal;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Payments.Tests;

[TestClass]
public sealed class ControllersTests
{
    [TestClass]
    public sealed class CospailControllerTests
    {
        private readonly Mock<ICospailService> _service = new();

        [TestMethod]
        public async Task GetMemberDebtByDocument_WhenFixedCodeIsNotPositive_ReturnsBadRequest()
        {
            var controller = new CospailController(_service.Object);

            var result = await controller.GetMemberDebtByDocument(
                0,
                "1234567",
                CancellationToken.None
            );

            result.Should().BeOfType<BadRequestObjectResult>();
            _service.Verify(
                x => x.GetMemberDebtByDocumentAsync(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never
            );
        }

        [TestMethod]
        public async Task GetMemberDebtByDocument_WhenDocumentIdIsEmpty_ReturnsBadRequest()
        {
            var controller = new CospailController(_service.Object);

            var result = await controller.GetMemberDebtByDocument(
                123,
                "  ",
                CancellationToken.None
            );

            result.Should().BeOfType<BadRequestObjectResult>();
            _service.Verify(
                x => x.GetMemberDebtByDocumentAsync(
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never
            );
        }

        [TestMethod]
        public async Task GetMemberDebtByDocument_WhenServiceSucceeds_ReturnsOkWithResult()
        {
            var controller = new CospailController(_service.Object);
            var expected = new GetMemberDebtByDocumentResponse { FixedCode = 123 };
            _service.Setup(x => x.GetMemberDebtByDocumentAsync(
                    123,
                    "1234567",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await controller.GetMemberDebtByDocument(
                123,
                "1234567",
                CancellationToken.None
            );

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeSameAs(expected);
        }

        [TestMethod]
        public async Task ConfirmPayment_WhenServiceSucceeds_ReturnsOkWithResult()
        {
            var controller = new CospailController(_service.Object);
            var request = new ConfirmPaymentRequestDto
            {
                FixedCode = 123,
                DocumentId = "1234567",
                Amount = 100m
            };
            var expected = new ConfirmPaymentResponseDto { Success = true };
            _service.Setup(x => x.ConfirmPaymentAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await controller.ConfirmPayment(request, CancellationToken.None);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeSameAs(expected);
        }

        [TestMethod]
        public async Task InitiatePayment_WhenServiceSucceeds_ReturnsOkWithResult()
        {
            var controller = new CospailController(_service.Object);
            var request = new InitiatePaymentRequestDto
            {
                FixedCode = 123,
                DocumentId = "1234567"
            };
            var expected = new PagoCospailResponseDto { PagoCospailId = Guid.NewGuid() };
            _service.Setup(x => x.InitiatePaymentAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await controller.InitiatePayment(request, CancellationToken.None);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeSameAs(expected);
        }

        [TestMethod]
        public async Task GetPaymentStatus_WhenServiceSucceeds_ReturnsOkWithResult()
        {
            var controller = new CospailController(_service.Object);
            var paymentId = Guid.NewGuid();
            var expected = new PagoCospailResponseDto { PagoCospailId = paymentId };
            _service.Setup(x => x.GetPaymentStatusAsync(paymentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await controller.GetPaymentStatus(paymentId, CancellationToken.None);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeSameAs(expected);
        }

        [TestMethod]
        public async Task GetActiveQr_WhenServiceReturnsQr_ReturnsOkWithResult()
        {
            var controller = new CospailController(_service.Object);
            var expected = new ActiveQrResponseDto { QrId = "qr-001" };
            _service.Setup(x => x.GetActiveQrAsync(123, "1234567", It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await controller.GetActiveQr(123, "1234567", CancellationToken.None);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeSameAs(expected);
        }

        [TestMethod]
        public async Task GetActiveQr_WhenMemberHasNoActiveQr_ReturnsNotFound()
        {
            var controller = new CospailController(_service.Object);
            _service.Setup(x => x.GetActiveQrAsync(123, "1234567", It.IsAny<CancellationToken>()))
                .ReturnsAsync((ActiveQrResponseDto?)null);

            var result = await controller.GetActiveQr(123, "1234567", CancellationToken.None);

            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetActiveQr_WhenFixedCodeIsNotPositive_ReturnsBadRequest()
        {
            var controller = new CospailController(_service.Object);

            var result = await controller.GetActiveQr(0, "1234567", CancellationToken.None);

            result.Should().BeOfType<BadRequestObjectResult>();
            _service.Verify(
                x => x.GetActiveQrAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }
    }

    [TestClass]
    public sealed class BancoEconomicoControllerTests
    {
        private readonly Mock<IBancoEconomicoService> _service = new();

        [TestMethod]
        public async Task GenerateQr_WhenServiceSucceeds_ReturnsOkWithResult()
        {
            var controller = new BancoEconomicoController(_service.Object);
            var request = new GenerateQrRequestDto
            {
                PagoCospailId = Guid.NewGuid(),
                BranchCode = "001"
            };
            var expected = new GenerateQrResponseDto { QrId = "qr-001", ResponseCode = 0 };
            _service.Setup(x => x.GenerateQrAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await controller.GenerateQr(request, CancellationToken.None);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeSameAs(expected);
        }

        [TestMethod]
        public async Task AnnulQr_WhenServiceSucceeds_ReturnsOkWithResult()
        {
            var controller = new BancoEconomicoController(_service.Object);
            var request = new AnnulQrRequestDto { PagoCospailId = Guid.NewGuid() };
            var expected = new AnnulQrResponseDto { ResponseCode = 0 };
            _service.Setup(x => x.AnnulQrAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await controller.AnnulQr(request, CancellationToken.None);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeSameAs(expected);
        }
    }

    [TestClass]
    public sealed class NotifyPaymentQrControllerTests
    {
        private readonly Mock<IBancoEconomicoService> _service = new();

        [TestMethod]
        public async Task NotifyPaymentQr_WhenServiceSucceeds_ReturnsOkWithSuccessCode()
        {
            var controller = new NotifyPaymentQrController(
                _service.Object,
                NullLogger<NotifyPaymentQrController>.Instance
            );
            _service.Setup(x => x.HandlePaymentNotificationAsync(
                    It.IsAny<NotifyPaymentQrRequestDto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NotifyPaymentQrResponseDto { ResponseCode = 0 });

            var result = await controller.NotifyPaymentQr(
                CreateNotification(),
                CancellationToken.None
            );

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<NotifyPaymentQrResponseDto>().Subject;
            response.ResponseCode.Should().Be(0);
        }

        [TestMethod]
        public async Task NotifyPaymentQr_WhenValidationFails_ReturnsOkWithResponseCode1()
        {
            var controller = new NotifyPaymentQrController(
                _service.Object,
                NullLogger<NotifyPaymentQrController>.Instance
            );
            _service.Setup(x => x.HandlePaymentNotificationAsync(
                    It.IsAny<NotifyPaymentQrRequestDto>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("payment.qrId", "payment.qrId es requerido.") }));

            var result = await controller.NotifyPaymentQr(
                CreateNotification(),
                CancellationToken.None
            );

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<NotifyPaymentQrResponseDto>().Subject;
            response.ResponseCode.Should().Be(1);
        }

        [TestMethod]
        public async Task NotifyPaymentQr_WhenArgumentInvalid_ReturnsOkWithResponseCode1()
        {
            var controller = new NotifyPaymentQrController(
                _service.Object,
                NullLogger<NotifyPaymentQrController>.Instance
            );
            _service.Setup(x => x.HandlePaymentNotificationAsync(
                    It.IsAny<NotifyPaymentQrRequestDto>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("No se encontró el QR."));

            var result = await controller.NotifyPaymentQr(
                CreateNotification(),
                CancellationToken.None
            );

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<NotifyPaymentQrResponseDto>().Subject;
            response.ResponseCode.Should().Be(1);
            response.Message.Should().Be("No se encontró el QR.");
        }

        [TestMethod]
        public async Task NotifyPaymentQr_WhenUnexpectedError_ReturnsOkWithResponseCode99()
        {
            var controller = new NotifyPaymentQrController(
                _service.Object,
                NullLogger<NotifyPaymentQrController>.Instance
            );
            _service.Setup(x => x.HandlePaymentNotificationAsync(
                    It.IsAny<NotifyPaymentQrRequestDto>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Error interno."));

            var result = await controller.NotifyPaymentQr(
                CreateNotification(),
                CancellationToken.None
            );

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<NotifyPaymentQrResponseDto>().Subject;
            response.ResponseCode.Should().Be(99);
        }

        private static NotifyPaymentQrRequestDto CreateNotification() => new()
        {
            Payment = new NotifyPaymentQrRequestDto.PaymentDto
            {
                QrId = "qr-001",
                TransactionId = "tx-001",
                PaymentDate = "2026-07-14",
                PaymentTime = "15:00:27",
                Currency = "BOB",
                Amount = 35.50m,
                SenderBankCode = "1016",
                SenderName = "Cliente",
                SenderAccount = "****1234",
                Description = "Pago",
                BranchCode = "001"
            }
        };
    }
}

