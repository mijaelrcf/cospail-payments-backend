using Application.Interfaces.Internal;
using Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application.DependencyInjection;

/// <summary>
/// Registro de dependencias de la capa Application.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICospailService, CospailService>();
        services.AddScoped<IBancoEconomicoService, BancoEconomicoService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminReportService, AdminReportService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
