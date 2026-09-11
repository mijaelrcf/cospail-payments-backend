using Application.DTOs.BancoEconomico.Requests;
using Application.DTOs.BancoEconomico.Responses;

namespace Application.Interfaces.External;

/// <summary>
/// Cliente externo para integración con Banco Económico.
/// </summary>
public interface IBancoEconomicoQrClient
{
    /// <summary>
    /// Autentica contra Banco Económico y devuelve el token de acceso.
    /// </summary>
    Task<AuthenticateResponseDto> AuthenticateAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera un código QR para pago utilizando el token de acceso obtenido previamente.
    /// </summary>
    Task<GenerateQrResponseDto> GenerateQrAsync(
        string bearerToken,
        GenerateQrBankRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Anula un código QR pendiente utilizando el token de acceso obtenido previamente.
    /// </summary>
    Task<AnnulQrResponseDto> AnnulQrAsync(
        string bearerToken,
        AnnulQrBankRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta el estado actual de un código QR (7.4 statusQR).
    /// </summary>
    Task<QrStatusResponseDto> GetQrStatusAsync(
        string bearerToken,
        string qrId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna el listado de QR pagados en una fecha yyyyMMdd (7.6 paidQR).
    /// </summary>
    Task<PaidQrListResponseDto> GetPaidQrListAsync(
        string bearerToken,
        string fecha,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta los movimientos de una cuenta por período (8.1 queryMovements).
    /// </summary>
    Task<QueryMovementsResponseDto> QueryMovementsAsync(
        string bearerToken,
        QueryMovementsBankRequestDto request,
        CancellationToken cancellationToken = default);
}