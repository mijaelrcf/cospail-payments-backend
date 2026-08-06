namespace Domain.Entities;

/// <summary>
/// Estado del socio consultado y de sus deudas en Cospail.
/// </summary>
public enum MemberDebtStatus
{
    /// <summary>
    /// El socio tiene deudas pendientes.
    /// </summary>
    HasDebt = 0,

    /// <summary>
    /// El socio no tiene deudas pendientes.
    /// </summary>
    NoDebt = 1,

    /// <summary>
    /// El documento no coincide con el código fijo.
    /// </summary>
    DocumentMismatch = 2,

    /// <summary>
    /// No existe un socio con el código fijo proporcionado.
    /// </summary>
    MemberNotFound = 3
}
