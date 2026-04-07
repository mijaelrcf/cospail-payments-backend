using Application.Interfaces.External;
using Infrastructure.Configuration;
using Infrastructure.ExternalServices.BancoEconomico;
using Infrastructure.ExternalServices.Cospail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection;

/// <summary>
/// Registro centralizado de dependencias de infraestructura.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<CospailSoapOptions>(
            configuration.GetSection(CospailSoapOptions.SectionName)
        );

        services.Configure<BancoEconomicoOptions>(
            configuration.GetSection(BancoEconomicoOptions.SectionName)
        );

        services.AddHttpClient<ICospailSoapClient, CospailSoapClient>();

        services.AddHttpClient<IBancoEconomicoQrClient, BancoEconomicoQrClient>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<BancoEconomicoOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            }
        );

        return services;
    }
}
