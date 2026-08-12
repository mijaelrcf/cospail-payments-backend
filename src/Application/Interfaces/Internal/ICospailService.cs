using Application.DTOs.Cospail.Requests;
using Application.DTOs.Cospail.Responses;

namespace Application.Interfaces.Internal;

/// <summary>
/// Servicio de aplicación para consultas relacionadas a Cospail.
/// </summary>
public interface ICospailService
{
    Task<GetMemberDebtByDocumentResponse> GetMemberDebtByDocumentAsync(
        int fixedCode,
        string documentId,
        CancellationToken cancellationToken = default
    );

    Task<ConfirmPaymentResponseDto> ConfirmPaymentAsync(
        ConfirmPaymentRequestDto request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Valida las deudas seleccionadas contra Cospail y persiste un pago
    /// agrupado con estado pendiente.
    /// </summary>
    Task<PagoCospailResponseDto> InitiatePaymentAsync(
        InitiatePaymentRequestDto request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Devuelve el estado actual de un pago agrupado y sus deudas.
    /// </summary>
    Task<PagoCospailResponseDto> GetPaymentStatusAsync(
        Guid pagoCospailId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Registra el cobro de una deuda en Cospail mediante grabarCobrosWEB.
    /// </summary>
    Task<RecordPaymentResponseDto> RecordDebtPaymentAsync(
        int creditNumber,
        int type,
        decimal amount,
        CancellationToken cancellationToken = default
    );
}
