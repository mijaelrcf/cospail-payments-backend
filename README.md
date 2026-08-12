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

- **Api** contiene los controladores `BancoEconomicoController`, `CospailController` y `NotifyPaymentQrController`, además del middleware de errores global.
- **Application** orquesta los casos de uso y define los contratos/DTOs en `src/Application/DTOs`.
- **Infrastructure** implementa las integraciones externas mediante `HttpClient`: SOAP manual para COSPAIL y HTTP JSON con Bearer token para Banco Económico.

Flujos principales:

1. Consulta de deudas: `GET /api/Cospail/member-debt-by-document` devuelve el socio y su lista de deudas vigentes en COSPAIL.
2. Inicio del pago: `POST /api/Cospail/payments/initiate` valida las deudas seleccionadas (una o más) contra COSPAIL y las persiste en PostgreSQL con estado `Pendiente`, agrupadas en un `PagoCospail`.
3. Generación QR: `POST /api/BancoEconomico/generate-qr` recibe el `pagoCospailId`, calcula el total de las deudas, se autentica ante Banco Económico y solicita el QR. El pago pasa a `QRGenerado`.
4. Notificación QR: Banco Económico llama a `POST /api/qrsimple/notifyPaymentQR`. El callback valida la notificación, marca el pago y sus deudas como `Pagado`, registra los datos del ordenante y la transacción en `notificaciones_pago_qr` (para saber quién pagó, importe, moneda y demás datos) y registra cada cobro en COSPAIL mediante `grabarCobrosWEB`. Si todos se registran, el pago pasa a `CospailRegistrado`; si alguno falla, queda en `Pagado` para reintento o conciliación.
5. Estado del pago: `GET /api/Cospail/payments/{pagoCospailId}` permite al frontend consultar el estado del pago y de cada deuda.

### Flujo de pago con QR (paso a paso)

El frontend siempre consume primero **initiate** y después **generate-qr**:

1. **`POST /api/Cospail/member-debt-by-document?fixedCode=123&documentId=CI123`** — consultar las deudas vigentes.
2. **`POST /api/Cospail/payments/initiate`** — el usuario selecciona una o más deudas:

   ```json
   {
     "fixedCode": 123,
     "documentId": "CI123",
     "debts": [
       { "creditNumber": 456, "type": 1, "amount": 100.00 },
       { "creditNumber": 789, "type": 3, "amount": 50.00 }
     ]
   }
   ```

   Respuesta: `{"pagoCospailId": "…", "totalAmount": 150.00, "status": "Pendiente", "debts": […]}`.
3. **`POST /api/BancoEconomico/generate-qr`** — se envía el `pagoCospailId` en lugar del importe (el total se calcula del pago):

   ```json
   {
     "transactionId": "tx-abc-123",
     "pagoCospailId": "…",
     "currency": "BOB",
     "dueDate": "2026-08-31",
     "description": "Pago de deudas Cospail",
     "branchCode": "001"
   }
   ```

   Respuesta: el QR emitido por Banco Económico (`qrId` y `qrImage`).
4. El usuario paga el QR; Banco Económico llama al **callback** `POST /api/qrsimple/notifyPaymentQR`:

   ```json
   {
     "payment": {
       "qrId": "22113001016800000017",
       "transactionId": "tx-abc-123",
       "paymentDate": "2026-08-11T00:00:00",
       "paymentTime": "10:23:45",
       "currency": "BOB",
       "amount": 150.00,
       "senderBankCode": "1016",
       "senderName": "CLIENTE DE PRUEBA 1234567",
       "senderDocumentId": "1234567",
       "senderAccount": "******5691",
       "description": "Pago de deudas Cospail",
       "branchCode": "001"
     }
   }
   ```

   Respuesta del callback (`200 OK`): `{"responseCode": 0, "message": ""}`.

   Si el `qrId` existe y el `transactionId` coincide, el pago y sus deudas pasan a `Pagado`
   y la notificación (importe, moneda, fecha/hora de pago y datos del ordenante) se persiste
   en `notificaciones_pago_qr`. El resultado se puede verificar con **`GET /api/Cospail/payments/{pagoCospailId}`** hasta que el pago llegue a `CospailRegistrado`. Con `responseCode: 1` los datos son inválidos o no coinciden con el QR; con `responseCode: 99`, un error interno.

> Sin `pagoCospailId`, `generate-qr` sigue generando un QR independiente con el `amount` y `currency` proporcionados, sin asociar deudas.

## Requisitos

- .NET SDK 10.0 o superior.
- Acceso de red a los servicios de COSPAIL y Banco Económico.
- Credenciales válidas de ambos proveedores.
- PostgreSQL 18.4 (o una versión compatible) para la persistencia de QR, pagos agrupados y deudas de Cospail.

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

`CospailSoap:Login` y `CospailSoap:Password` se incluyen en el sobre SOAP de `grabarCobrosWEB`. `AccountCredit` recibido en la solicitud de QR es reemplazado por el valor configurado en el servidor.

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
| GET | `/api/Cospail/member-debt-by-document` | Consulta deudas y estado del socio por código fijo y CI/NIT. |
| POST | `/api/Cospail/payments/initiate` | Valida y persiste un pago agrupado de una o más deudas. |
| GET | `/api/Cospail/payments/{pagoCospailId}` | Consulta el estado de un pago agrupado y sus deudas. |
| POST | `/api/BancoEconomico/generate-qr` | Genera un QR de cobro (con `pagoCospailId` suma el total de las deudas). |
| POST | `/api/Cospail/payments/confirm` | Valida y registra un cobro individual en COSPAIL. |
| POST | `/api/qrsimple/notifyPaymentQR` | Callback de pago de Banco Económico. Marca el pago `Pagado` y registra los cobros en COSPAIL. |
| GET | `/health` | Health check (incluye conectividad con la base de datos). |

Los errores globales se entregan como `application/problem+json`: 400 para argumentos inválidos, 404 para recursos no encontrados y 500 para errores no controlados. El callback QR es la excepción: siempre devuelve 200 y utiliza `responseCode` (`0`, `1` o `99`).

En el callback, `payment.paymentDate` admite `yyyy-MM-dd` o `yyyy-MM-ddTHH:mm:ss` y `payment.branchCode` es opcional.



