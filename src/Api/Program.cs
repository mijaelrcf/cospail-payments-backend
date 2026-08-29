using System.Text;
using Api.Middleware;
using Application.DependencyInjection;
using Application.Options;
using Infrastructure.DependencyInjection;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
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

        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "Token JWT del panel de administración. Copiar el token devuelto por POST /api/admin/auth/login. " +
                    "Se acepta con o sin el prefijo 'Bearer '."
            }
        );

        options.AddSecurityRequirement(
            document =>
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference(
                            referenceId: "Bearer",
                            hostDocument: document,
                            externalResource: null
                        ),
                        new List<string>()
                    }
                }
        );
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

// Autenticación JWT del panel de administración
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

var secretKeyBytes = Encoding.UTF8.GetBytes(authOptions.SecretKey);
if (string.IsNullOrWhiteSpace(authOptions.SecretKey) || secretKeyBytes.Length < 32)
{
    var effective = secretKeyBytes.Length < 32 ? secretKeyBytes.Length * 8 : 0;
    throw new InvalidOperationException(
        "La configuración 'Auth:SecretKey' es obligatoria y debe tener al menos 256 bits. " +
        $"El valor efectivamente cargado solo tiene {effective} bits. " +
        "Definela en los secrets de desarrollo o en una variable de entorno (Auth__SecretKey); " +
        "si definiste varias, recuerda que tienen precedencia las variables de entorno y luego los secrets."
    );
}

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = authOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(authOptions.SecretKey)
            ),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers.Authorization.ToString();
                if (
                    string.IsNullOrEmpty(context.Token)
                    && !string.IsNullOrWhiteSpace(authHeader)
                )
                {
                    var candidate = authHeader.Trim();
                    if (!candidate.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = candidate;
                    }
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                context
                    .HttpContext.RequestServices.GetRequiredService<ILogger<Program>>()
                    .LogWarning("Autenticación JWT fallida: {Motivo}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context
                    .HttpContext.RequestServices.GetRequiredService<ILogger<Program>>()
                    .LogWarning(
                        "Petición no autenticada en {Path}. Error: {Error} Desc: {Desc}",
                        context.Request.Path,
                        context.Error,
                        context.ErrorDescription
                    );

                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                context.Response.Headers["WWW-Authenticate"] = "Bearer";

                var isInvalidToken = string.Equals(
                    context.Error,
                    "invalid_token",
                    StringComparison.OrdinalIgnoreCase
                );

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = isInvalidToken
                        ? "El token proporcionado es inválido o ha expirado. Inicia sesión nuevamente para obtener un token vigente."
                        : "Se debe incluir un token Bearer en el encabezado Authorization para acceder a este recurso. Obtenlo mediante POST /api/admin/auth/login.",
                    Instance = context.Request.Path
                };

                return context.Response.WriteAsJsonAsync(
                    problem,
                    cancellationToken: context.HttpContext.RequestAborted
                );
            }
        };
    });

builder
    .Services.AddAuthorizationBuilder();

// Health checks: verifica conectividad con la base de datos
builder.Services.AddHealthChecks().AddDbContextCheck<PaymentsDbContext>("database");

var app = builder.Build();

// Middleware global de errores
app.UseMiddleware<GlobalExceptionHandler>();

var enableSwagger = app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled");

if (enableSwagger)
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
