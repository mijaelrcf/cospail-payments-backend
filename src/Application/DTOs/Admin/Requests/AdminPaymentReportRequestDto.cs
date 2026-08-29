using Domain.Entities;

namespace Application.DTOs.Admin.Requests;

/// <summary>
/// Filtros y paginación para el reporte de pagos del panel de administración.
/// </summary>
public sealed class AdminPaymentReportRequestDto
{
    /// <summary>Fecha de inicio del rango (opcional, sobre CreatedAtUtc).</summary>
    public DateTime? From { get; set; }

    /// <summary>Fecha de fin del rango (opcional, inclusiva hasta fin de día).</summary>
    public DateTime? To { get; set; }

    /// <summary>Estado del pago (opcional; si se omite se devuelven todos los estados).</summary>
    public PagoCospailStatus? Status { get; set; }

    /// <summary>Código fijo del socio (opcional).</summary>
    public int? FixedCode { get; set; }

    /// <summary>Documento de identidad o NIT del socio (opcional, búsqueda parcial).</summary>
    public string? DocumentId { get; set; }

    /// <summary>Página solicitada, desde 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Tamaño de página.</summary>
    public int PageSize { get; set; } = 20;
}
