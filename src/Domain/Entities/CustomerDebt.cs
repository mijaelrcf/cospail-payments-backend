namespace Domain.Entities;

/// <summary>
/// Representa la deuda de un cliente obtenida desde Cospail.
/// </summary>
public class CustomerDebt
{
    public int FixedCode { get; set; }
    public string? CustomerIdentifier { get; set; }
    public decimal Amount { get; set; }
    public DateTime RetrievedAt { get; set; }
}
