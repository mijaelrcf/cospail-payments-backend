# Cospail.Payments.Backend

Web API en **.NET 10** que centraliza la consulta y el registro de cobros de COSPAIL, y la emisión de cobros QR a través de Banco Económico.

La especificación OpenAPI se genera automáticamente con Swagger y está disponible en `/swagger` al ejecutar la aplicación en modo Development.

## Arquitectura

La solución sigue una separación de responsabilidades compatible con Clean Architecture/DDD:

```text
Cliente HTTP / Banco Económico (callback)
              |
              v
        Api (Controllers)
              |
              v
 Application (servicios, DTOs e interfaces)
              |
              v
Infrastructure (clientes HTTP/SOAP y configuración)
       |                         |
       v                         v
SOAP/SOA COSPAIL          API Gateway Banco Económico
```

- **Api** contiene los controladores `BancoEconomicoController`, `CospailSoapController` y `NotifyPaymentQrController`, además del middleware de errores global.
- **Application** orquesta los casos de uso y define los contratos/DTOs en `src/Application/DTOs`.
- **Infrastructure** implementa las integraciones externas mediante `HttpClient`: SOAP manual para COSPAIL y HTTP JSON con Bearer token para Banco Económico.

Flujos principales:

1. Consulta y confirmación: el API consulta la deuda en COSPAIL; antes de registrar el pago valida socio, documento, crédito, tipo e importe.
2. Generación QR: el API se autentica con Banco Económico usando las credenciales del servidor, obtiene un token Bearer y solicita el QR. La respuesta contiene `qrImage`.
3. Notificación QR: Banco Económico llama a `POST /api/qrsimple/notifyPaymentQR`. El QR pendiente se marca como pagado de forma idempotente; la acreditación automática en COSPAIL permanece como trabajo de una fase posterior.

## Requisitos

- .NET SDK 10.0 o superior.
- Acceso de red a los servicios de COSPAIL y Banco Económico.
- Credenciales válidas de ambos proveedores.
- PostgreSQL 18.4 (o una versión compatible) para la persistencia de QR.

## Configuración

Las opciones se enlazan desde `ExternalServices`. Nunca versionar credenciales reales; para desarrollo se recomienda Secret Manager y para despliegues, variables de entorno del proveedor de hosting.

Variables de entorno equivalentes (ASP.NET Core reemplaza `:` por `__`):

```powershell
$env:ExternalServices__CospailSoap__BaseUrl = 'https://ws.cospail.com.bo/wstest/wsco.asmx'
$env:ExternalServices__CospailSoap__Login = 'USUARIO_COSPAIL'
$env:ExternalServices__CospailSoap__Password = 'PASSWORD_COSPAIL'

$env:ExternalServices__BanEcoApi__BaseUrl = 'https://apimktdesa.baneco.com.bo/ApiGateway/'
$env:ExternalServices__BanEcoApi__UserName = 'USUARIO_BANECO'
$env:ExternalServices__BanEcoApi__EncryptedPassword = 'PASSWORD_CIFRADO_ENTREGADO_POR_BANECO'
$env:ExternalServices__BanEcoApi__AccountCredit = 'CUENTA_CIFRADA_O_CONFIGURADA_POR_BANECO'
$env:ConnectionStrings__PaymentsDatabase = 'Host=localhost;Port=5432;Database=cospail_payments;Username=postgres;Password=TU_PASSWORD'
```

También pueden guardarse como secretos de desarrollo:

```powershell
dotnet user-secrets init --project src/Api
dotnet user-secrets set "ExternalServices:CospailSoap:BaseUrl" "https://ws.cospail.com.bo/wstest/wsco.asmx" --project src/Api
dotnet user-secrets set "ExternalServices:CospailSoap:Login" "USUARIO_COSPAIL" --project src/Api
dotnet user-secrets set "ExternalServices:CospailSoap:Password" "PASSWORD_COSPAIL" --project src/Api
dotnet user-secrets set "ExternalServices:BanEcoApi:BaseUrl" "https://apimktdesa.baneco.com.bo/ApiGateway/" --project src/Api
dotnet user-secrets set "ExternalServices:BanEcoApi:UserName" "USUARIO_BANECO" --project src/Api
dotnet user-secrets set "ExternalServices:BanEcoApi:EncryptedPassword" "PASSWORD_CIFRADO_BANECO" --project src/Api
dotnet user-secrets set "ExternalServices:BanEcoApi:AccountCredit" "CUENTA_BANECO" --project src/Api
dotnet user-secrets set "ConnectionStrings:PaymentsDatabase" "Host=localhost;Port=5432;Database=cospail_payments;Username=postgres;Password=TU_PASSWORD" --project src/Api
```

`CospailSoap:Login` y `CospailSoap:Password` forman parte de la configuración, aunque el cliente SOAP actual no los incluye en el sobre SOAP. `AccountCredit` recibido en la solicitud de QR es reemplazado por el valor configurado en el servidor.

### Migraciones PostgreSQL

La aplicación valida que exista `ConnectionStrings:PaymentsDatabase`, pero no crea ni migra la base automáticamente. Instala una versión 10.x de `dotnet-ef` y aplica las migraciones explícitamente:

```powershell
dotnet tool install --global dotnet-ef --version 10.*
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

Para generar una migración futura:

```powershell
dotnet ef migrations add NombreDeLaMigracion --project src/Infrastructure --startup-project src/Api --output-dir Persistence/Migrations
```

Las pruebas de integración no usan una base por defecto. Para ejecutarlas contra una base de pruebas aislada:

```powershell
$env:PAYMENTS_TEST_CONNECTION_STRING = 'Host=localhost;Port=5432;Database=cospail_payments_test;Username=postgres;Password=TU_PASSWORD'
dotnet test tests/Payments.Tests/Payments.Tests.csproj
```

## Ejecución rápida

```powershell
dotnet restore
dotnet build
dotnet run --project src/Api --launch-profile https
```

En el entorno `Development`, Swagger queda disponible en `/swagger`. Si prefieres HTTP, usa `--launch-profile http`. La política CORS actual permite únicamente `http://localhost:5173`; ajuste `FrontendPolicy` en `src/Api/Program.cs` si el frontend se ejecuta en otro origen.

## Endpoints

| Método | Ruta | Propósito |
| --- | --- | --- |
| POST | `/api/BancoEconomico/generate-qr` | Genera un QR de cobro. |
| GET | `/api/CospailSoap/debt/{fixedCode}` | Consulta deuda por código fijo. |
| GET | `/api/CospailSoap/member-debt-by-document` | Consulta deuda por código fijo y CI/NIT. |
| POST | `/api/CospailSoap/payments/confirm` | Valida y registra un cobro en COSPAIL. |
| POST | `/api/qrsimple/notifyPaymentQR` | Callback de pago de Banco Económico. |
| GET | `/health` | Health check (incluye conectividad con la base de datos). |

Los errores globales se entregan como `application/problem+json`: 400 para argumentos inválidos, 404 para recursos no encontrados y 500 para errores no controlados. El callback QR es la excepción: siempre devuelve 200 y utiliza `responseCode` (`0`, `1` o `99`).


