using Domain.Entities;

namespace Application.DTOs.Cospail.Responses;

/// <summary>
/// QR vigente (pendiente y no vencido) asociado a un socio, para volver a
/// mostrarlo hasta que se pague o se anule.
/// </summary>
public sealed class ActiveQrResponseDto
{
    /// <summary>
    /// Identificador del pago agrupado al que pertenece el QR.
    /// </summary>
    public Guid PagoCospailId { get; set; }

    /// <summary>
    /// Identificador único del QR emitido por Banco Económico.
    /// </summary>
    public string QrId { get; set; } = string.Empty;

    /// <summary>
    /// Imagen del QR en base64, cuando está disponible.
    /// </summary>
    public string? QrImage { get; set; }

    /// <summary>
    /// Importe solicitado para el QR.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Moneda del cobro.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de vencimiento del QR.
    /// </summary>
    public DateOnly DueDate { get; set; }

    /// <summary>
    /// Estado actual del QR.
    /// </summary>
    public PagoQrStatus Status { get; set; }

    /// <summary>
    /// Fecha y hora UTC en que se emitió el QR.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
}
