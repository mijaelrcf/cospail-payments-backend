using Domain.Entities;

namespace Application.DTOs.Cospail.Responses;

/// <summary>
/// Información de una deuda dentro de un pago agrupado de Cospail.
/// </summary>
public sealed class PagoDebtResponseDto
{
    public int CreditNumber { get; set; }
    public int Type { get; set; }
    public int NoticeNumber { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string Period { get; set; } = string.Empty;
    public string? MemberName { get; set; }
    public decimal Amount { get; set; }
    public DeudaCospailStatus Status { get; set; }
}