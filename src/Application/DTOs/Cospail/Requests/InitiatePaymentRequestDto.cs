namespace Application.DTOs.Cospail.Requests;

/// <summary>
/// Solicitud para iniciar el pago de una o varias deudas de Cospail.
/// </summary>
public sealed class InitiatePaymentRequestDto
{
    public int FixedCode { get; set; }
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// Deudas seleccionadas que se pagan juntas mediante un único QR. Puede
    /// incluir una o más deudas según lo permitido por el negocio.
    /// </summary>
    public List<InitiatePaymentDebtDto> Debts { get; set; } = new();
}