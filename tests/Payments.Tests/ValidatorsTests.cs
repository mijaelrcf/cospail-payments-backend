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
                TransactionId = "tx-001",
                Currency = "BOB",
                Amount = 35.50m,
                DueDate = "2026-07-31",
                BranchCode = "001"
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenTransactionIdMissing_ReturnsError()
        {
            var request = new GenerateQrRequestDto
            {
                TransactionId = "",
                Currency = "BOB",
                Amount = 35.50m,
                DueDate = "2026-07-31"
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.TransactionId));
        }

        [TestMethod]
        public void Validate_WhenAmountNotPositive_ReturnsError()
        {
            var request = new GenerateQrRequestDto
            {
                TransactionId = "tx-001",
                Currency = "BOB",
                Amount = 0,
                DueDate = "2026-07-31"
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.Amount));
        }

        [TestMethod]
        public void Validate_WhenDueDateHasInvalidFormat_ReturnsError()
        {
            var request = new GenerateQrRequestDto
            {
                TransactionId = "tx-001",
                Currency = "BOB",
                Amount = 35.50m,
                DueDate = "31/07/2026"
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.DueDate));
        }

        [TestMethod]
        public void Validate_WhenCurrencyIsNotSupported_ReturnsError()
        {
            var request = new GenerateQrRequestDto
            {
                TransactionId = "tx-001",
                Currency = "EUR",
                Amount = 35.50m,
                DueDate = "2026-07-31"
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.Currency));
        }

        [TestMethod]
        public void Validate_WhenBranchCodeExceedsMaxLength_ReturnsError()
        {
            var request = new GenerateQrRequestDto
            {
                TransactionId = "tx-001",
                Currency = "BOB",
                Amount = 35.50m,
                DueDate = "2026-07-31",
                BranchCode = "123456"
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.BranchCode));
        }

        [TestMethod]
        public void Validate_WhenTransactionIdExceedsMaxLength_ReturnsError()
        {
            var request = new GenerateQrRequestDto
            {
                TransactionId = new string('x', 101),
                Currency = "BOB",
                Amount = 35.50m,
                DueDate = "2026-07-31"
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.TransactionId));
        }

        [TestMethod]
        public void Validate_WhenDescriptionExceedsMaxLength_ReturnsError()
        {
            var request = new GenerateQrRequestDto
            {
                TransactionId = "tx-001",
                Currency = "BOB",
                Amount = 35.50m,
                DueDate = "2026-07-31",
                Description = new string('x', 501)
            };

            var result = _validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(x => x.PropertyName == nameof(request.Description));
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
}
