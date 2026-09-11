using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.Cospail.Requests;
using Application.Validators;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Payments.Tests;

[TestClass]
public sealed class ValidatorsTests
{
    [TestClass]
    public sealed class GenerateQrRequestDtoValidatorTests
    {
        private readonly GenerateQrRequestDtoValidator _validator = new();

        [TestMethod]
        public void Validate_WhenRequestIsValid_ReturnsNoErrors()
        {
            var request = new GenerateQrRequestDto
            {
                PagoCospailId = Guid.NewGuid(),
                BranchCode = "001"
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenPagoCospailIdIsEmpty_ReturnsError()
        {
            var request = new GenerateQrRequestDto
            {
                PagoCospailId = Guid.Empty
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.PagoCospailId));
        }

        [TestMethod]
        public void Validate_WhenBranchCodeExceedsMaxLength_ReturnsError()
        {
            var request = new GenerateQrRequestDto
            {
                PagoCospailId = Guid.NewGuid(),
                BranchCode = "123456"
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.BranchCode));
        }
    }

    [TestClass]
    public sealed class AnnulQrRequestDtoValidatorTests
    {
        private readonly AnnulQrRequestDtoValidator _validator = new();

        [TestMethod]
        public void Validate_WhenPagoCospailIdIsPresent_ReturnsNoErrors()
        {
            var request = new AnnulQrRequestDto
            {
                PagoCospailId = Guid.NewGuid()
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenPagoCospailIdIsEmpty_ReturnsError()
        {
            var request = new AnnulQrRequestDto
            {
                PagoCospailId = Guid.Empty
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.PagoCospailId));
        }
    }

    [TestClass]
    public sealed class NotifyPaymentQrRequestDtoValidatorTests
    {
        private readonly NotifyPaymentQrRequestDtoValidator _validator = new();

        [TestMethod]
        public void Validate_WhenPaymentIsValid_ReturnsNoErrors()
        {
            var request = CreateValidNotification();

            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenPaymentIsNull_ReturnsError()
        {
            var request = new NotifyPaymentQrRequestDto();

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.Payment));
        }

        [TestMethod]
        public void Validate_WhenQrIdMissing_ReturnsError()
        {
            var request = CreateValidNotification();
            request.Payment!.QrId = "";

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == "Payment.QrId");
        }

        [TestMethod]
        public void Validate_WhenPaymentDateHasInvalidFormat_ReturnsError()
        {
            var request = CreateValidNotification();
            request.Payment!.PaymentDate = "14/07/2026";

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == "Payment.PaymentDate");
        }

        [TestMethod]
        public void Validate_WhenPaymentDateHasDatetimeFormat_ReturnsNoErrors()
        {
            var request = CreateValidNotification();
            request.Payment!.PaymentDate = "2026-07-14T00:00:00";

            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenPaymentDateIsIsoWithZulu_ReturnsNoErrors()
        {
            var request = CreateValidNotification();
            request.Payment!.PaymentDate = "2026-09-07T04:00:00Z";
            request.Payment!.PaymentTime = "15:13:57";

            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenDescriptionIsMissing_ReturnsNoErrors()
        {
            var request = CreateValidNotification();
            request.Payment!.Description = "";

            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenBranchCodeIsMissing_ReturnsNoErrors()
        {
            var request = CreateValidNotification();
            request.Payment!.BranchCode = "";

            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenPaymentTimeHasInvalidFormat_ReturnsError()
        {
            var request = CreateValidNotification();
            request.Payment!.PaymentTime = "3:00 PM";

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == "Payment.PaymentTime");
        }

        [TestMethod]
        public void Validate_WhenAmountIsZero_ReturnsError()
        {
            var request = CreateValidNotification();
            request.Payment!.Amount = 0;

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == "Payment.Amount");
        }

        private static NotifyPaymentQrRequestDto CreateValidNotification() => new()
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

    [TestClass]
    public sealed class ConfirmPaymentRequestDtoValidatorTests
    {
        private readonly ConfirmPaymentRequestDtoValidator _validator = new();

        [TestMethod]
        public void Validate_WhenRequestIsValid_ReturnsNoErrors()
        {
            var request = new ConfirmPaymentRequestDto
            {
                FixedCode = 12345,
                DocumentId = "1234567",
                CreditNumber = 1,
                Type = 1,
                Amount = 100.00m
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenFixedCodeIsZero_ReturnsError()
        {
            var request = new ConfirmPaymentRequestDto
            {
                FixedCode = 0,
                DocumentId = "1234567",
                Amount = 100.00m
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.FixedCode));
        }

        [TestMethod]
        public void Validate_WhenDocumentIdMissing_ReturnsError()
        {
            var request = new ConfirmPaymentRequestDto
            {
                FixedCode = 12345,
                DocumentId = "",
                Amount = 100.00m
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.DocumentId));
        }

        [TestMethod]
        public void Validate_WhenAmountNotPositive_ReturnsError()
        {
            var request = new ConfirmPaymentRequestDto
            {
                FixedCode = 12345,
                DocumentId = "1234567",
                Amount = -5
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.Amount));
        }
    }

    [TestClass]
    public sealed class InitiatePaymentRequestDtoValidatorTests
    {
        private readonly InitiatePaymentRequestDtoValidator _validator = new();

        [TestMethod]
        public void Validate_WhenRequestIsValid_ReturnsNoErrors()
        {
            var request = CreateRequest();

            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenNoDebts_ReturnsError()
        {
            var request = CreateRequest();
            request.Debts = new List<InitiatePaymentDebtDto>();

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.Debts));
        }

        [TestMethod]
        public void Validate_WhenManyDebts_ReturnsNoErrors()
        {
            var request = CreateRequest();
            request.Debts = Enumerable
                .Range(1, 6)
                .Select(i => new InitiatePaymentDebtDto { CreditNumber = i, Type = 1, Amount = 10m })
                .ToList();

            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenDebtCreditNumberIsZero_ReturnsError()
        {
            var request = CreateRequest();
            request.Debts = new List<InitiatePaymentDebtDto>
            {
                new() { CreditNumber = 0, Type = 1, Amount = 10m }
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == "Debts[0].CreditNumber");
        }

        [TestMethod]
        public void Validate_WhenFixedCodeIsZero_ReturnsError()
        {
            var request = CreateRequest();
            request.FixedCode = 0;

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.FixedCode));
        }

        private static InitiatePaymentRequestDto CreateRequest() => new()
        {
            FixedCode = 12345,
            DocumentId = "1234567",
            Debts = new List<InitiatePaymentDebtDto>
            {
                new() { CreditNumber = 1, Type = 1, Amount = 100m }
            }
        };
    }

    [TestClass]
    public sealed class QrStatusRequestDtoValidatorTests
    {
        private readonly QrStatusRequestDtoValidator _validator = new();

        [TestMethod]
        public void Validate_WhenQrIdIsPresent_ReturnsNoErrors()
        {
            var result = _validator.Validate(new QrStatusRequestDto { QrId = "qr-001" });

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenQrIdIsEmpty_ReturnsError()
        {
            var request = new QrStatusRequestDto { QrId = "  " };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.QrId));
        }
    }

    [TestClass]
    public sealed class PaidQrListRequestDtoValidatorTests
    {
        private readonly PaidQrListRequestDtoValidator _validator = new();

        [TestMethod]
        public void Validate_WhenFechaIsValid_ReturnsNoErrors()
        {
            var result = _validator.Validate(new PaidQrListRequestDto { Fecha = "20260719" });

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenFechaHasInvalidFormat_ReturnsError()
        {
            var request = new PaidQrListRequestDto { Fecha = "19-07-2026" };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.Fecha));
        }

        [TestMethod]
        public void Validate_WhenFechaIsFuture_ReturnsError()
        {
            var request = new PaidQrListRequestDto
            {
                Fecha = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)).ToString("yyyyMMdd")
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.Fecha));
        }
    }

    [TestClass]
    public sealed class QueryMovementsRequestDtoValidatorTests
    {
        private readonly QueryMovementsRequestDtoValidator _validator = new();

        [TestMethod]
        public void Validate_WhenRangeIsValid_ReturnsNoErrors()
        {
            var result = _validator.Validate(new QueryMovementsRequestDto
            {
                StartDate = "2026-07-01",
                EndDate = "2026-07-19"
            });

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenStartDateIsAfterEndDate_ReturnsError()
        {
            var request = new QueryMovementsRequestDto
            {
                StartDate = "2026-07-19",
                EndDate = "2026-07-01"
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
        }

        [TestMethod]
        public void Validate_WhenDateHasInvalidFormat_ReturnsError()
        {
            var request = new QueryMovementsRequestDto
            {
                StartDate = "01/07/2026",
                EndDate = "2026-07-19"
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.StartDate));
        }
    }
}
