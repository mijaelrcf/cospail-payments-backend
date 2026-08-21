using Application.Interfaces.External;
using Microsoft.Extensions.Options;

namespace Infrastructure.Configuration;

/// <summary>
/// Adaptador que expone las opciones de generación de QR de Banco Económico
/// a la capa Application.
/// </summary>
public sealed class BancoEconomicoQrSettings(IOptions<BancoEconomicoOptions> options) : IBancoEconomicoQrSettings
{
    public int QrValidityHours => options.Value.QrValidityHours;
}
