using Application.DTOs.Admin.Requests;
using Application.DTOs.Admin.Responses;
using Application.Interfaces.Internal;
using Application.Interfaces.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

/// <summary>
/// Servicio de aplicación de reportes para el panel de administración.
/// </summary>
public sealed class AdminReportService(IPaymentsDbContext dbContext) : IAdminReportService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<AdminPaymentReportResponseDto> GetPaymentReportAsync(
        AdminPaymentReportRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? DefaultPageSize : request.PageSize, 1, MaxPageSize);

        IQueryable<PagoCospail> query = dbContext.PagosCospail.Include(x => x.Deudas);

        if (request.From.HasValue)
        {
            var fromUtc = NormalizeUtc(request.From.Value);
            query = query.Where(x => x.CreatedAtUtc >= fromUtc);
        }

        if (request.To.HasValue)
        {
            var toUtc = NormalizeUtc(request.To.Value).AddDays(1); // inclusivo hasta fin de día
            query = query.Where(x => x.CreatedAtUtc < toUtc);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.FixedCode.HasValue)
        {
            var fixedCode = request.FixedCode.Value;
            query = query.Where(x => x.FixedCode == fixedCode);
        }

        if (!string.IsNullOrWhiteSpace(request.DocumentId))
        {
            var documentId = request.DocumentId.Trim();
            query = query.Where(x => x.DocumentId.Contains(documentId));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new AdminPaymentReportResponseDto
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            PageCount = pageCount,
            Items = items.Select(ToReportItem).ToList()
        };
    }

    public async Task<AdminPaymentDetailDto> GetPaymentDetailAsync(
        Guid pagoCospailId,
        CancellationToken cancellationToken = default
    )
    {
        var pagoCospail = await dbContext
            .PagosCospail.Include(x => x.Deudas)
            .Include(x => x.Qr)
            .SingleOrDefaultAsync(x => x.Id == pagoCospailId, cancellationToken);

        if (pagoCospail is null)
        {
            throw new KeyNotFoundException(
                "No se encontró un pago con el pagoCospailId proporcionado."
            );
        }

        AdminQrNotificationDto? notification = null;
        if (pagoCospail.PagoQrId.HasValue)
        {
            var notif = await dbContext
                .NotificacionesPagoQr.Where(n => n.PagoQrId == pagoCospail.PagoQrId.Value)
                .OrderByDescending(n => n.ReceivedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (notif is not null)
            {
                notification = new AdminQrNotificationDto
                {
                    QrId = notif.QrId,
                    TransactionId = notif.TransactionId,
                    PaymentDate = notif.PaymentDate,
                    PaymentTime = notif.PaymentTime,
                    PaymentAtUtc = notif.PaymentAtUtc,
                    Currency = notif.Currency,
                    Amount = notif.Amount,
                    SenderBankCode = notif.SenderBankCode,
                    SenderName = notif.SenderName,
                    SenderDocumentId = notif.SenderDocumentId,
                    SenderAccount = notif.SenderAccount,
                    Description = notif.Description,
                    BranchCode = notif.BranchCode,
                    ReceivedAtUtc = notif.ReceivedAtUtc
                };
            }
        }

        return ToDetail(pagoCospail, notification);
    }

    private static AdminPaymentReportItemDto ToReportItem(PagoCospail p) =>
        new()
        {
            PagoCospailId = p.Id,
            FixedCode = p.FixedCode,
            DocumentId = p.DocumentId,
            MemberName = p.MemberName,
            TotalAmount = p.TotalAmount,
            Status = p.Status.ToString(),
            CreatedAtUtc = p.CreatedAtUtc,
            UpdatedAtUtc = p.UpdatedAtUtc,
            Debts = p.Deudas.Select(ToDebt).ToList()
        };

    private static AdminPaymentDetailDto ToDetail(
        PagoCospail p,
        AdminQrNotificationDto? notification
    ) =>
        new()
        {
            PagoCospailId = p.Id,
            FixedCode = p.FixedCode,
            DocumentId = p.DocumentId,
            MemberName = p.MemberName,
            TotalAmount = p.TotalAmount,
            Status = p.Status.ToString(),
            CreatedAtUtc = p.CreatedAtUtc,
            UpdatedAtUtc = p.UpdatedAtUtc,
            Debts = p.Deudas.Select(ToDebt).ToList(),
            QrNotification = notification
        };

    private static AdminPaymentDebtDto ToDebt(DeudaCospail d) =>
        new()
        {
            CreditNumber = d.CreditNumber,
            Type = d.Type,
            NoticeNumber = d.NoticeNumber,
            Year = d.Year,
            Month = d.Month,
            Period = d.Period,
            Amount = d.Amount,
            Status = d.Status.ToString()
        };

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
