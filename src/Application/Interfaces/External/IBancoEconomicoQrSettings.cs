namespace Application.Interfaces.External;

/// <summary>
/// Configuración del servidor utilizada al generar los QR de Banco Económico.
/// </summary>
public interface IBancoEconomicoQrSettings
{
    /// <summary>
    /// Horas de vigencia del QR a partir de ahora. Con 0 el QR vence el día de
    /// hoy (hora Bolivia); con 24 vence mañana, con 48 en dos días, etc.
    /// </summary>
    int QrValidityHours { get; }
}
