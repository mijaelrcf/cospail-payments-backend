using Application.Interfaces.Internal;
using Application.Options;
using Application.Services;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Application.DependencyInjection;

/// <summary>
/// Registro de dependencias de la capa Application.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddScoped<ICospailService, CospailService>();
        services.AddScoped<IBancoEconomicoService, BancoEconomicoService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminReportService, AdminReportService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddSingleton<IValidateOptions<AuthOptions>, AuthOptionsValidator>();
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
