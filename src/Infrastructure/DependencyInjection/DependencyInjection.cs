using Application.Interfaces.External;
using Application.Interfaces.Persistence;
using Infrastructure.Configuration;
using Infrastructure.ExternalServices.BancoEconomico;
using Infrastructure.ExternalServices.Cospail;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
        var connectionString = configuration.GetConnectionString("PaymentsDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexión 'ConnectionStrings:PaymentsDatabase' es obligatoria."
            );
        }

        services.AddDbContext<PaymentsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IPaymentsDbContext>(provider =>
            provider.GetRequiredService<PaymentsDbContext>()
        );

        services.Configure<CospailSoapOptions>(
            configuration.GetSection(CospailSoapOptions.SectionName)
        );

        services.Configure<BancoEconomicoOptions>(
            configuration.GetSection(BancoEconomicoOptions.SectionName)
        );

        services.AddSingleton<IBancoEconomicoQrSettings, BancoEconomicoQrSettings>();

        services.AddHttpClient<ICospailSoapClient, CospailSoapClient>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<CospailSoapOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            }
        );

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
