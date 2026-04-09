using Application.Interfaces.Internal;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.DependencyInjection;

/// <summary>
/// Registro de dependencias de la capa Application.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICospailSoapService, CospailSoapService>();
        services.AddScoped<IBancoEconomicoService, BancoEconomicoService>();

        return services;
    }
}
