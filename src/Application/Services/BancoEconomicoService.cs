using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;
using Application.Interfaces.External;
using Application.Interfaces.Internal;
using Application.Interfaces.Persistence;
using Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Application.Services;

/// <summary>
/// Servicio de aplicación para operaciones con Banco Económico.
/// </summary>
public sealed class BancoEconomicoService(
    IBancoEconomicoQrClient bancoEconomicoQrClient,
    IPaymentsDbContext dbContext,
    IValidator<GenerateQrRequestDto> generateQrValidator,
    IValidator<NotifyPaymentQrRequestDto> notifyPaymentValidator,
    IValidator<AnnulQrRequestDto> annulQrValidator,
    ICospailService cospailService,
    IBancoEconomicoQrSettings qrSettings,
    ILogger<BancoEconomicoService> logger
) : IBancoEconomicoService
{
    /// <inheritdoc />
    public async Task<GenerateQrResponseDto> GenerateQrAsync(
        GenerateQrRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        await generateQrValidator.ValidateAndThrowAsync(request, cancellationToken);

        var pagoCospail = await dbContext
            .PagosCospail.Include(x => x.Deudas)
            .SingleOrDefaultAsync(x => x.Id == request.PagoCospailId, cancellationToken);

        if (pagoCospail is null)
        {
            throw new ArgumentException("pagoCospailId no existe.");
        }

        if (pagoCospail.Status != PagoCospailStatus.Pendiente)
        {
            throw new ArgumentException(
                pagoCospail.Status == PagoCospailStatus.Anulado
                    ? "El pago fue anulado. Inicie un nuevo pago."
                    : "El pago ya tiene un QR asociado."
            );
        }

        if (await MemberHasActiveQrAsync(pagoCospail, cancellationToken))
        {
            throw new ArgumentException(
                "El socio ya tiene un QR pendiente de pago. Debe pagarlo o anularlo antes de generar otro."
            );
        }

        var bankRequest = BuildBankRequest(pagoCospail, request.BranchCode);

        logger.LogInformation(
            "Solicitando generación de QR. TransactionId: {TransactionId}, PagoCospailId: {PagoCospailId}, Amount: {Amount}, Currency: {Currency}",
            bankRequest.TransactionId,
            pagoCospail.Id,
            bankRequest.Amount,
            bankRequest.Currency
        );

        var auth = await bancoEconomicoQrClient.AuthenticateAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(auth.Token))
        {
            throw new InvalidOperationException(
                "No se recibió token de autenticación desde Banco Económico."
            );
        }

        var response = await bancoEconomicoQrClient.GenerateQrAsync(
            auth.Token,
            bankRequest,
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(response.QrId))
        {
            throw new InvalidOperationException("Banco Económico no devolvió un qrId para el QR generado.");
        }

        var pagoQr = new PagoQr(
            bankRequest.TransactionId,
            response.QrId.Trim(),
            bankRequest.Amount,
            bankRequest.Currency,
            DateOnly.ParseExact(bankRequest.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
            bankRequest.SingleUse,
            bankRequest.ModifyAmount,
            bankRequest.Description?.Trim(),
            bankRequest.BranchCode?.Trim(),
            response.QrImage,
            DateTime.UtcNow
        );

        dbContext.PagosQr.Add(pagoQr);

        if (pagoCospail is not null && !pagoCospail.MarkAsQrGenerated(pagoQr.Id))
        {
            throw new ArgumentException("El pago ya tiene un QR asociado.");
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ArgumentException("Ya existe un QR registrado para el transactionId proporcionado.");
        }

        return response;
    }

    /// <summary>
    /// Construye el payload para Banco Económico a partir del pago de Cospail.
    /// El importe, la moneda, la transacción y el vencimiento se resuelven en el servidor.
    /// </summary>
    private GenerateQrBankRequestDto BuildBankRequest(PagoCospail pagoCospail, string? branchCode)
    {
        var expiresAtUtc = DateTime.UtcNow.AddHours(qrSettings.QrValidityHours);
        var dueDate = DateOnly.FromDateTime(BoliviaTime.FromUtc(expiresAtUtc));

        return new GenerateQrBankRequestDto
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            Currency = "BOB",
            Amount = pagoCospail.TotalAmount,
            Description = BuildDescription(pagoCospail),
            DueDate = dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            SingleUse = true,
            ModifyAmount = false,
            BranchCode = branchCode?.Trim()
        };
    }

    private static string BuildDescription(PagoCospail pagoCospail) =>
        string.Join(",", pagoCospail.Deudas.Select(x => x.CreditNumber).Distinct());

    /// <inheritdoc />
    public async Task<AnnulQrResponseDto> AnnulQrAsync(
        AnnulQrRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        await annulQrValidator.ValidateAndThrowAsync(request, cancellationToken);

        var pagoCospail = await dbContext
            .PagosCospail.Include(x => x.Qr)
            .Include(x => x.Deudas)
            .SingleOrDefaultAsync(x => x.Id == request.PagoCospailId, cancellationToken);

        if (pagoCospail is null)
        {
            throw new ArgumentException("pagoCospailId no existe.");
        }

        if (pagoCospail.Qr is null)
        {
            throw new ArgumentException("El pago no tiene un QR asociado.");
        }

        var qr = pagoCospail.Qr;

        if (qr.Status == PagoQrStatus.Anulado)
        {
            return new AnnulQrResponseDto { ResponseCode = 0, Message = string.Empty };
        }

        if (qr.Status == PagoQrStatus.Pagado)
        {
            throw new ArgumentException("El QR ya fue pagado y no puede anularse.");
        }

        if (pagoCospail.Status != PagoCospailStatus.QRGenerado)
        {
            throw new InvalidOperationException("El pago no está en estado QR generado.");
        }

        logger.LogInformation(
            "Solicitando anulación de QR. QrId: {QrId}, PagoCospailId: {PagoCospailId}",
            qr.QrId,
            pagoCospail.Id
        );

        var auth = await bancoEconomicoQrClient.AuthenticateAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(auth.Token))
        {
            throw new InvalidOperationException(
                "No se recibió token de autenticación desde Banco Económico."
            );
        }

        var response = await bancoEconomicoQrClient.AnnulQrAsync(
            auth.Token,
            new AnnulQrBankRequestDto { QrId = qr.QrId },
            cancellationToken
        );

        if (!qr.MarkAsAnnulled())
        {
            throw new InvalidOperationException("No se pudo anular el QR en su estado actual.");
        }

        if (!pagoCospail.MarkAsAnulado())
        {
            throw new InvalidOperationException("No se pudo anular el pago.");
        }

        foreach (var deuda in pagoCospail.Deudas)
        {
            if (!deuda.MarkAsAnulado())
            {
                logger.LogWarning(
                    "No se anuló la deuda {CreditNumber} del pago {PagoCospailId} porque está en estado {Status}.",
                    deuda.CreditNumber,
                    pagoCospail.Id,
                    deuda.Status
                );
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    private async Task<bool> MemberHasActiveQrAsync(
        PagoCospail pagoCospail,
        CancellationToken cancellationToken
    )
    {
        var today = BoliviaTime.Today();

        return await dbContext
            .PagosCospail.Where(x =>
                x.FixedCode == pagoCospail.FixedCode
                && x.DocumentId == pagoCospail.DocumentId
                && x.Id != pagoCospail.Id
                && x.Qr != null
                && x.Qr.Status == PagoQrStatus.Pendiente
                && x.Qr.DueDate >= today
            )
            .AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<NotifyPaymentQrResponseDto> HandlePaymentNotificationAsync(
        NotifyPaymentQrRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Payment is not null)
        {
            request.Payment.Currency = request.Payment.Currency?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        await notifyPaymentValidator.ValidateAndThrowAsync(request, cancellationToken);

        var payment = request.Payment!;

        using var scope = logger.BeginScope(
            new Dictionary<string, object>
            {
                ["QrId"] = payment.QrId,
                ["TransactionId"] = payment.TransactionId,
                ["Amount"] = payment.Amount,
                ["Currency"] = payment.Currency,
                ["BranchCode"] = payment.BranchCode
            }
        );

        logger.LogInformation("Notificación de pago QR recibida desde Banco Económico.");

        var pagoQr = await dbContext.PagosQr.SingleOrDefaultAsync(x => x.QrId == (payment.QrId ?? string.Empty).Trim(), cancellationToken);

        if (pagoQr is null)
        {
            throw new ArgumentException("No existe un QR registrado para payment.qrId.");
        }

        if (!pagoQr.ModifyAmount && payment.Amount != pagoQr.Amount)
        {
            throw new ArgumentException("payment.amount no coincide con el importe del QR.");
        }

        if (!string.Equals(pagoQr.Currency, payment.Currency, StringComparison.Ordinal))
        {
            throw new ArgumentException("payment.currency no coincide con la moneda del QR.");
        }

        var paymentAtUtc = ParsePaymentDateTimeUtc(payment.PaymentDate, payment.PaymentTime);

        if (pagoQr.Status == PagoQrStatus.Pendiente)
        {
            pagoQr.MarkAsPaid(paymentAtUtc);
        }

        dbContext.NotificacionesPagoQr.Add(new NotificacionPagoQr(
            pagoQr,
            (payment.QrId ?? string.Empty).Trim(),
            (payment.TransactionId ?? string.Empty).Trim(),
            (payment.PaymentDate ?? string.Empty).Trim(),
            (payment.PaymentTime ?? string.Empty).Trim(),
            paymentAtUtc,
            payment.Currency,
            payment.Amount,
            (payment.SenderBankCode ?? string.Empty).Trim(),
            (payment.SenderName ?? string.Empty).Trim(),
            (payment.SenderDocumentId ?? string.Empty).Trim(),
            (payment.SenderAccount ?? string.Empty).Trim(),
            (payment.Description ?? string.Empty).Trim(),
            string.IsNullOrWhiteSpace(payment.BranchCode) ? null : payment.BranchCode.Trim(),
            DateTime.UtcNow
        ));

        var pagoCospail = await dbContext
            .PagosCospail.Include(x => x.Deudas)
            .SingleOrDefaultAsync(x => x.PagoQrId == pagoQr.Id, cancellationToken);

        if (pagoCospail is not null)
        {
            if (pagoCospail.Status == PagoCospailStatus.Anulado)
            {
                logger.LogWarning(
                    "Se recibió notificación de pago para el QR {QrId} cuyo pago {PagoCospailId} está anulado. No se registrarán cobros.",
                    pagoQr.QrId,
                    pagoCospail.Id
                );
            }
            else
            {
                await RegisterDebtsInCospailAsync(pagoCospail, cancellationToken);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new NotifyPaymentQrResponseDto
        {
            ResponseCode = 0,
            Message = string.Empty
        };
    }

    private async Task<bool> RegisterDebtsInCospailAsync(
        PagoCospail pagoCospail,
        CancellationToken cancellationToken
    )
    {
        if (pagoCospail.Status is PagoCospailStatus.CospailRegistrado or PagoCospailStatus.Anulado)
        {
            return false;
        }

        var debtsToRegister = pagoCospail
            .Deudas.Where(x => x.Status != DeudaCospailStatus.CospailRegistrado)
            .ToList();

        if (debtsToRegister.Count == 0)
        {
            pagoCospail.MarkAsCospailRegistrado();
            return true;
        }

        var allRegistered = true;

        foreach (var deuda in debtsToRegister)
        {
            try
            {
                var response = await cospailService.RecordDebtPaymentAsync(
                    deuda.CreditNumber,
                    deuda.Type,
                    deuda.Amount,
                    cancellationToken
                );

                if (response.Success)
                {
                    deuda.MarkAsCospailRegistrado();
                }
                else
                {
                    deuda.MarkAsPagado();
                    allRegistered = false;
                    logger.LogWarning(
                        "Cospail no registró el cobro de la deuda {CreditNumber} del pago {PagoCospailId}. Respuesta: {Message}",
                        deuda.CreditNumber,
                        pagoCospail.Id,
                        response.Message
                    );
                }
            }
            catch (Exception ex)
            {
                deuda.MarkAsPagado();
                allRegistered = false;
                logger.LogError(
                    ex,
                    "Error registrando en Cospail el cobro de la deuda {CreditNumber} del pago {PagoCospailId}.",
                    deuda.CreditNumber,
                    pagoCospail.Id
                );
            }
        }

        if (allRegistered)
        {
            pagoCospail.MarkAsCospailRegistrado();
        }
        else
        {
            pagoCospail.MarkAsPagado();
        }

        return true;
    }

    private static DateTime ParsePaymentDateTimeUtc(string? paymentDate, string? paymentTime)
    {
        var date = ParsePaymentDate(paymentDate);
        var time = ParsePaymentTime(paymentTime, paymentDate);
        var localPaymentDateTime = date.ToDateTime(time, DateTimeKind.Unspecified);

        // Banco Económico reporta la fecha y hora local de Bolivia (UTC-04:00).
        return new DateTimeOffset(localPaymentDateTime, TimeSpan.FromHours(-4)).UtcDateTime;
    }

    private static DateOnly ParsePaymentDate(string? paymentDate)
    {
        var value = (paymentDate ?? string.Empty).Trim();

        // Tolerante: si el banco no envía fecha, se usa la fecha actual de Bolivia.
        if (string.IsNullOrWhiteSpace(value))
        {
            return BoliviaTime.Today();
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        if (DateTime.TryParseExact(
            value,
            "yyyy-MM-ddTHH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        // Formatos ISO 8601 que envía el banco en producción, ej. "2026-09-07T04:00:00Z"
        // o con offset "2026-09-07T00:00:00-04:00" (con o sin milisegundos).
        // Se extrae solo la parte de fecha; la hora se toma de paymentTime.
        if (value.StartsWith("20", StringComparison.Ordinal) || value.StartsWith("19", StringComparison.Ordinal))
        {
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            {
                return DateOnly.FromDateTime(dto.DateTime);
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallback))
            {
                return DateOnly.FromDateTime(fallback);
            }
        }

        throw new ArgumentException("payment.paymentDate debe tener formato yyyy-MM-dd o ISO 8601 (yyyy-MM-ddTHH:mm:ss).");
    }

    private static TimeOnly ParsePaymentTime(string? paymentTime, string? paymentDate)
    {
        var timeValue = (paymentTime ?? string.Empty).Trim();

        if (TimeOnly.TryParseExact(timeValue, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return time;
        }

        if (TimeOnly.TryParseExact(timeValue, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var shortTime))
        {
            return shortTime;
        }

        // Tolerante: si no hay hora separada, se intenta extraer del paymentDate
        // (ej. "2026-09-07T04:00:00Z") o se usa medianoche.
        if (string.IsNullOrWhiteSpace(timeValue))
        {
            var dateValue = (paymentDate ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(dateValue)
                && DateTimeOffset.TryParse(dateValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            {
                return TimeOnly.FromDateTime(dto.DateTime);
            }

            if (!string.IsNullOrWhiteSpace(dateValue)
                && DateTime.TryParse(dateValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return TimeOnly.FromDateTime(dt);
            }

            return TimeOnly.MinValue;
        }

        if (TimeOnly.TryParse(timeValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        throw new ArgumentException("payment.paymentTime debe tener formato HH:mm:ss.");
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        for (var current = exception.InnerException; current is not null; current = current.InnerException)
        {
            if (
                current.Message.Contains("23505", StringComparison.Ordinal)
                || current.Message.Contains("duplicate key value", StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }
        }

        return false;
    }
}
