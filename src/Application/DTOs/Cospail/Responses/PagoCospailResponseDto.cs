using Domain.Entities;

namespace Application.DTOs.Cospail.Responses;

/// <summary>
/// Representa un pago agrupado de deudas de Cospail y su estado actual.
/// </summary>
public sealed class PagoCospailResponseDto
{
    public Guid PagoCospailId { get; set; }
    public int FixedCode { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public string? MemberName { get; set; }
    public decimal TotalAmount { get; set; }
    public PagoCospailStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<PagoDebtResponseDto> Debts { get; set; } = new();
}