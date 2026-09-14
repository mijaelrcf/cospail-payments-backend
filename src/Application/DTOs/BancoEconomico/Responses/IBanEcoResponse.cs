namespace Application.DTOs.BancoEconomico.Responses;

/// <summary>
/// Respuesta funcional de Banco Económico.
/// Convención del banco: <c>ResponseCode == 0</c> es éxito.
/// </summary>
public interface IBanEcoResponse
{
    int ResponseCode { get; set; }

    string? Message { get; set; }
}
