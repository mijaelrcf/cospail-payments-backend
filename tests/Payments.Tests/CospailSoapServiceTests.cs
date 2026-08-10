using Application.DTOs.Cospail.Common;
using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;
using Application.Interfaces.External;
using Application.Services;
using Application.Validators;
using Domain.Entities;
using FluentAssertions;
using FluentValidation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Payments.Tests;

[TestClass]
public sealed class CospailSoapServiceTests
{
    [TestClass]
    public sealed class GetDebtAsyncTests
    {
        [TestMethod]
        public async Task GetDebtAsync_WhenFixedCodeIsValid_ReturnsClientResponse()
        {
            var client = new Mock<ICospailSoapClient>();
            var expected = new CospailDebtResponseDto { FixedCode = 123, Amount = 50m };
            client.Setup(x => x.GetDebtByFixedCodeAsync(123, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);
            var service = CreateService(client);

            var result = await service.GetDebtAsync(123);

            result.Should().BeSameAs(expected);
        }

        [TestMethod]
        public async Task GetDebtAsync_WhenFixedCodeIsNotPositive_Throws()
        {
            var service = CreateService(new Mock<ICospailSoapClient>());

            var act = () => service.GetDebtAsync(0);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*mayor a cero*");
        }
    }

    [TestClass]
    public sealed class GetMemberDebtByDocumentAsyncTests
    {
        [TestMethod]
        public async Task GetMemberDebtByDocumentAsync_WhenInputsAreValid_ReturnsClientResponse()
        {
            var client = new Mock<ICospailSoapClient>();
            var expected = new GetMemberDebtByDocumentResponse
            {
                FixedCode = 123,
                DocumentId = "1234567",
                Status = MemberDebtStatus.HasDebt
            };
            client.Setup(x => x.GetMemberDebtByDocumentAsync(
                    123,
                    "1234567",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);
            var service = CreateService(client);

            var result = await service.GetMemberDebtByDocumentAsync(123, "1234567");

            result.Should().BeSameAs(expected);
        }

        [TestMethod]
        public async Task GetMemberDebtByDocumentAsync_WhenFixedCodeIsNotPositive_Throws()
        {
            var service = CreateService(new Mock<ICospailSoapClient>());

            var act = () => service.GetMemberDebtByDocumentAsync(0, "1234567");

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*mayor a cero*");
        }
    }

    [TestClass]
    public sealed class ConfirmPaymentAsyncTests
    {
        private static Mock<ICospailSoapClient> CreateClientWithMember(
            MemberDebtStatus status = MemberDebtStatus.HasDebt
        )
        {
            var client = new Mock<ICospailSoapClient>();
            var response = new GetMemberDebtByDocumentResponse
            {
                FixedCode = 123,
                DocumentId = "1234567",
                MemberName = "Juan Perez",
                Status = status
            };

            if (status == MemberDebtStatus.HasDebt)
            {
                response.Debts.Add(new DebtItemDto
                {
                    NoticeNumber = 1,
                    CreditNumber = 5,
                    Type = 1,
                    Year = 2026,
                    Month = 7,
                    Period = "2026-07",
                    Amount = 100.00m
                });
            }

            client.Setup(x => x.GetMemberDebtByDocumentAsync(
                    123,
                    "1234567",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            client.Setup(x => x.RecordPaymentAsync(
                    It.IsAny<RecordPaymentRequestDto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RecordPaymentResponseDto
                {
                    Success = true,
                    Message = "Cobro registrado.",
                    RawResult = "<result>ok</result>"
                });

            return client;
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WhenMemberHasMatchingDebt_RecordsPaymentAndReturnsSuccess()
        {
            var client = CreateClientWithMember();
            var service = CreateService(client);

            var result = await service.ConfirmPaymentAsync(CreateRequest());

            result.Success.Should().BeTrue();
            result.Message.Should().Be("Cobro registrado.");
            result.FixedCode.Should().Be(123);
            result.DocumentId.Should().Be("1234567");
            result.MemberName.Should().Be("Juan Perez");

            client.Verify(x => x.RecordPaymentAsync(
                    It.Is<RecordPaymentRequestDto>(r =>
                        r.CreditNumber == 5
                        && r.Type == 1
                        && r.Amount == 100.00m
                        && r.PaymentDate.Kind == DateTimeKind.Unspecified),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WhenRequestIsNull_Throws()
        {
            var service = CreateService(new Mock<ICospailSoapClient>());

            var act = () => service.ConfirmPaymentAsync(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WhenRequestIsInvalid_ThrowsValidationException()
        {
            var service = CreateService(new Mock<ICospailSoapClient>());
            var invalidRequest = CreateRequest();
            invalidRequest.FixedCode = 0;
            invalidRequest.Amount = -5;

            var act = () => service.ConfirmPaymentAsync(invalidRequest);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WhenMemberNotFound_Throws()
        {
            var service = CreateService(CreateClientWithMember(MemberDebtStatus.MemberNotFound));

            var act = () => service.ConfirmPaymentAsync(CreateRequest());

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no existe*");
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WhenDocumentMismatch_Throws()
        {
            var service = CreateService(CreateClientWithMember(MemberDebtStatus.DocumentMismatch));

            var act = () => service.ConfirmPaymentAsync(CreateRequest());

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no coincide*");
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WhenMemberHasNoDebt_Throws()
        {
            var service = CreateService(CreateClientWithMember(MemberDebtStatus.NoDebt));

            var act = () => service.ConfirmPaymentAsync(CreateRequest());

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*deudas*");
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WhenDebtDoesNotMatch_Throws()
        {
            var client = CreateClientWithMember();
            var service = CreateService(client);
            var request = CreateRequest();
            request.CreditNumber = 99;
            request.Amount = 999.00m;

            var act = () => service.ConfirmPaymentAsync(request);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*no coincide*");

            client.Verify(x => x.RecordPaymentAsync(
                    It.IsAny<RecordPaymentRequestDto>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    private static CospailSoapService CreateService(Mock<ICospailSoapClient> client) =>
        new(client.Object, new ConfirmPaymentRequestDtoValidator());

    private static ConfirmPaymentRequestDto CreateRequest() => new()
    {
        FixedCode = 123,
        DocumentId = "1234567",
        CreditNumber = 5,
        Type = 1,
        Amount = 100.00m
    };
}
