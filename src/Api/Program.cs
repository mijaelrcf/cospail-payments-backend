using Api.Middleware;
using Application.DependencyInjection;
using Infrastructure.DependencyInjection;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

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
builder
    .Services
    .AddCors(options =>
    {
        options.AddPolicy(
            "FrontendPolicy",
            policy =>
            {
                policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod();
            }
        );
    });

// Registrar capas
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Middleware global de errores
app.UseMiddleware<GlobalExceptionHandler>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Usar la política de CORS para permitir el acceso desde el frontend
app.UseCors("FrontendPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
