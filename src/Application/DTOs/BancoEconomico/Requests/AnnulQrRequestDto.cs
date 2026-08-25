namespace Application.DTOs.BancoEconomico.Requests;

/// <summary>
/// Request para anular el QR pendiente de un pago de deudas de Cospail.
/// </summary>
public sealed class AnnulQrRequestDto
{
    /// <summary>
    /// Identificador del pago agrupado cuyo QR se desea anular.
    /// </summary>
    public Guid PagoCospailId { get; set; }
}
