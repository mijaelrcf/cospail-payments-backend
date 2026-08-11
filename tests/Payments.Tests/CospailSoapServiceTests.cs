using Application.DTOs.Cospail.Common;
using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;
using Application.Interfaces.External;
using Application.Services;
using Application.Validators;
using Domain.Entities;
using FluentAssertions;
using FluentValidation;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*no existe*");
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WhenDocumentMismatch_Throws()
        {
            var service = CreateService(CreateClientWithMember(MemberDebtStatus.DocumentMismatch));

            var act = () => service.ConfirmPaymentAsync(CreateRequest());

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*no coincide*");
        }

        [TestMethod]
        public async Task ConfirmPaymentAsync_WhenMemberHasNoDebt_Throws()
        {
            var service = CreateService(CreateClientWithMember(MemberDebtStatus.NoDebt));

            var act = () => service.ConfirmPaymentAsync(CreateRequest());

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*deudas*");
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

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*no coincide*");

            client.Verify(x => x.RecordPaymentAsync(
                    It.IsAny<RecordPaymentRequestDto>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    [TestClass]
    public sealed class InitiatePaymentAsyncTests
    {
        private static Mock<ICospailSoapClient> CreateClientWithDebts(params DebtItemDto[] debts)
        {
            var client = new Mock<ICospailSoapClient>();
            var response = new GetMemberDebtByDocumentResponse
            {
                FixedCode = 123,
                DocumentId = "1234567",
                MemberName = "Juan Perez",
                Status = MemberDebtStatus.HasDebt
            };
            response.Debts.AddRange(debts);
            client.Setup(x => x.GetMemberDebtByDocumentAsync(
                    123,
                    "1234567",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            return client;
        }

        private static DebtItemDto CreateDebt(int creditNumber, decimal amount) => new()
        {
            NoticeNumber = creditNumber,
            CreditNumber = creditNumber,
            Type = 1,
            Year = 2026,
            Month = 7,
            Period = "2026-07",
            MemberName = "Juan Perez",
            Amount = amount
        };

        [TestMethod]
        public async Task InitiatePaymentAsync_WhenDebtMatches_PersistsPendingPayment()
        {
            await using var db = CreateInMemoryDb();
            var service = CreateService(
                CreateClientWithDebts(CreateDebt(5, 100.00m)),
                db
            );

            var result = await service.InitiatePaymentAsync(CreateRequest());

            result.PagoCospailId.Should().NotBeEmpty();
            result.FixedCode.Should().Be(123);
            result.MemberName.Should().Be("Juan Perez");
            result.TotalAmount.Should().Be(100.00m);
            result.Status.Should().Be(PagoCospailStatus.Pendiente);
            result.Debts.Should().ContainSingle(x =>
                x.CreditNumber == 5 && x.Status == DeudaCospailStatus.Pendiente
            );

            var stored = await db
                .PagosCospail.Include(x => x.Deudas)
                .SingleAsync(x => x.Id == result.PagoCospailId);
            stored.Status.Should().Be(PagoCospailStatus.Pendiente);
            stored.TotalAmount.Should().Be(100.00m);
            stored.Deudas.Should().HaveCount(1);
            stored.Deudas.Single().Period.Should().Be("2026-07");
        }

        [TestMethod]
        public async Task InitiatePaymentAsync_WhenMultipleDebtsMatch_ComputesTotal()
        {
            await using var db = CreateInMemoryDb();
            var client = CreateClientWithDebts(CreateDebt(5, 100.00m), CreateDebt(6, 50.00m));
            var service = CreateService(client, db);
            var request = CreateRequest();
            request.Debts = new List<InitiatePaymentDebtDto>
            {
                new() { CreditNumber = 5, Type = 1, Amount = 100.00m },
                new() { CreditNumber = 6, Type = 1, Amount = 50.00m }
            };

            var result = await service.InitiatePaymentAsync(request);

            result.TotalAmount.Should().Be(150.00m);
            result.Debts.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task InitiatePaymentAsync_WhenMemberNotFound_Throws()
        {
            var client = new Mock<ICospailSoapClient>();
            client.Setup(x => x.GetMemberDebtByDocumentAsync(
                    123,
                    "1234567",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetMemberDebtByDocumentResponse
                {
                    FixedCode = 123,
                    DocumentId = "1234567",
                    Status = MemberDebtStatus.MemberNotFound
                });
            var service = CreateService(client);

            var act = () => service.InitiatePaymentAsync(CreateRequest());

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*no existe*");
        }

        [TestMethod]
        public async Task InitiatePaymentAsync_WhenDebtDoesNotMatch_Throws()
        {
            var service = CreateService(CreateClientWithDebts(CreateDebt(5, 100.00m)));
            var request = CreateRequest();
            request.Debts = new List<InitiatePaymentDebtDto>
            {
                new() { CreditNumber = 99, Type = 1, Amount = 999.00m }
            };

            var act = () => service.InitiatePaymentAsync(request);

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*no coincide*");
        }

        [TestMethod]
        public async Task InitiatePaymentAsync_WhenManyDebtsAreSelected_AllowsThem()
        {
            await using var db = CreateInMemoryDb();
            var debts = Enumerable.Range(1, 6).Select(i => CreateDebt(i, 10m)).ToArray();
            var service = CreateService(CreateClientWithDebts(debts), db);
            var request = CreateRequest();
            request.Debts = Enumerable
                .Range(1, 6)
                .Select(i => new InitiatePaymentDebtDto { CreditNumber = i, Type = 1, Amount = 10m })
                .ToList();

            var result = await service.InitiatePaymentAsync(request);

            result.Debts.Should().HaveCount(6);
            result.TotalAmount.Should().Be(60m);
        }

        private static InitiatePaymentRequestDto CreateRequest() => new()
        {
            FixedCode = 123,
            DocumentId = "1234567",
            Debts = new List<InitiatePaymentDebtDto>
            {
                new() { CreditNumber = 5, Type = 1, Amount = 100.00m }
            }
        };
    }

    [TestClass]
    public sealed class GetPaymentStatusAsyncTests
    {
        [TestMethod]
        public async Task GetPaymentStatusAsync_WhenPaymentExists_ReturnsItsState()
        {
            await using var db = CreateInMemoryDb();
            var pagoCospail = new PagoCospail(123, "1234567", "Juan Perez", 100.00m, DateTime.UtcNow);
            pagoCospail.AddDeuda(
                new DeudaCospail(123, "1234567", "Juan Perez", 5, 1, 5, 2026, 7, "2026-07", 100.00m)
            );
            db.PagosCospail.Add(pagoCospail);
            await db.SaveChangesAsync();
            var service = CreateService(new Mock<ICospailSoapClient>(), db);

            var result = await service.GetPaymentStatusAsync(pagoCospail.Id);

            result.PagoCospailId.Should().Be(pagoCospail.Id);
            result.Status.Should().Be(PagoCospailStatus.Pendiente);
            result.Debts.Should().ContainSingle();
        }

        [TestMethod]
        public async Task GetPaymentStatusAsync_WhenPaymentDoesNotExist_Throws()
        {
            var service = CreateService(new Mock<ICospailSoapClient>());

            var act = () => service.GetPaymentStatusAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }

    [TestClass]
    public sealed class RecordDebtPaymentAsyncTests
    {
        [TestMethod]
        public async Task RecordDebtPaymentAsync_RecordsPaymentWithBoliviaDateTime()
        {
            var client = new Mock<ICospailSoapClient>();
            client.Setup(x => x.RecordPaymentAsync(
                    It.IsAny<RecordPaymentRequestDto>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RecordPaymentResponseDto
                {
                    Success = true,
                    Message = "Cobro registrado.",
                    RawResult = "1"
                });
            var service = CreateService(client);

            var result = await service.RecordDebtPaymentAsync(5, 1, 100.00m);

            result.Success.Should().BeTrue();
            client.Verify(x => x.RecordPaymentAsync(
                    It.Is<RecordPaymentRequestDto>(r =>
                        r.CreditNumber == 5
                        && r.Type == 1
                        && r.Amount == 100.00m
                        && r.PaymentDate.Kind == DateTimeKind.Unspecified),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    private static CospailSoapService CreateService(
        Mock<ICospailSoapClient> client,
        PaymentsDbContext? db = null
    ) =>
        new(
            client.Object,
            db ?? CreateInMemoryDb(),
            new ConfirmPaymentRequestDtoValidator(),
            new InitiatePaymentRequestDtoValidator()
        );

    private static PaymentsDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PaymentsDbContext(options);
    }

    private static ConfirmPaymentRequestDto CreateRequest() => new()
    {
        FixedCode = 123,
        DocumentId = "1234567",
        CreditNumber = 5,
        Type = 1,
        Amount = 100.00m
    };
}
