using Api.Middleware;
using Application.DependencyInjection;
using Infrastructure.DependencyInjection;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog: reemplaza los proveedores por defecto y escribe en consola y en archivo (logs/api-YYYYMMDD.log)
builder.Logging.ClearProviders();
builder
    .Host
    .UseSerilog(
        (context, services, configuration) =>
            configuration
                .ReadFrom
                .Configuration(context.Configuration)
                .ReadFrom
                .Services(services)
                .Enrich
                .FromLogContext()
                .WriteTo
                .Console()
                .WriteTo
                .File("logs/api-.log", rollingInterval: RollingInterval.Day)
    );

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder
    .Services
    .AddSwaggerGen(options =>
    {
        options.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title = "COSPAIL Payments API",
                Version = "v1",
                Description =
                    "API HTTP que integra las consultas y el registro de cobros del servicio SOAP de COSPAIL con la generación y notificación de pagos QR de Banco Económico."
            }
        );

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath);
    });

//Add Cors policy to allow frontend access
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder
    .Services
    .AddCors(options =>
    {
        options.AddPolicy(
            "FrontendPolicy",
            policy =>
            {
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
            }
        );
    });

// Registrar capas
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Health checks: verifica conectividad con la base de datos
builder.Services.AddHealthChecks().AddDbContextCheck<PaymentsDbContext>("database");

var app = builder.Build();

// Middleware global de errores
app.UseMiddleware<GlobalExceptionHandler>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Confiar en los encabezados X-Forwarded-* del proxy (Render termina TLS y envía HTTP interno).
// Sin esto, UseHttpsRedirection detrás de un proxy externo causa bucles de redirección.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();

// Usar la política de CORS para permitir el acceso desde el frontend
app.UseCors("FrontendPolicy");

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
