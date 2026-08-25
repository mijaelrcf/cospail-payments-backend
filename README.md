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
2. QR activo: `GET /api/Cospail/payments/active-qr` devuelve el QR vigente del socio (estado `Pendiente` y no vencido) con su imagen, o `404` si no tiene ninguno; el frontend lo usa para volver a mostrar un QR pendiente en lugar de permitir iniciar otro pago.
3. Inicio del pago: `POST /api/Cospail/payments/initiate` valida las deudas seleccionadas (una o más) contra COSPAIL y las persiste en PostgreSQL con estado `Pendiente`, agrupadas en un `PagoCospail`. Si el socio ya tiene un QR activo, devuelve `400`.
4. Generación QR: `POST /api/BancoEconomico/generate-qr` recibe el `pagoCospailId`, calcula el total de las deudas, se autentica ante Banco Económico y solicita el QR. El pago pasa a `QRGenerado`.
5. Notificación QR: Banco Económico llama a `POST /api/qrsimple/notifyPaymentQR`. El callback valida la notificación, marca el pago y sus deudas como `Pagado`, registra los datos del ordenante y la transacción en `notificaciones_pago_qr` (para saber quién pagó, importe, moneda y demás datos) y registra cada cobro en COSPAIL mediante `grabarCobrosWEB`. Si todos se registran, el pago pasa a `CospailRegistrado`; si alguno falla, queda en `Pagado` para reintento o conciliación.
6. Estado del pago: `GET /api/Cospail/payments/{pagoCospailId}` permite al frontend consultar el estado del pago y de cada deuda.
7. Anulación: `POST /api/BancoEconomico/annul-qr` cancela el QR ante Banco Económico (`DELETE api/qrsimple/cancelQR`) y deja el intento en estado terminal: QR, pago y sus deudas quedan `Anulado`. Las deudas siguen debiéndose en Cospail y pueden incluirse en un nuevo pago (nuevo `initiate`).

### Flujo de pago con QR (paso a paso)

El frontend siempre consume primero **active-qr**, luego **initiate** y después **generate-qr**:

0. **`GET /api/Cospail/payments/active-qr?fixedCode=123&documentId=CI123`** — si responde `200`, el socio ya tiene un QR vigente: mostrar ese QR (con su botón de anular) en lugar de iniciar otro pago. Si responde `404`, continuar con el flujo normal.
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
3. **`POST /api/BancoEconomico/generate-qr`** — solo recibe el `pagoCospailId` (obtenido en el paso anterior) y opcionalmente `branchCode`; el resto de los datos del cobro se resuelven en la API:

   ```json
   {
     "pagoCospailId": "…",
     "branchCode": "001"
   }
   ```

   La API calcula el importe total del pago, fija `currency: "BOB"`, genera el
   `transactionId`, define `dueDate` según `ExternalServices:BanEcoApi:QrValidityHours`
   (0 = vence hoy, hora Bolivia; 24 = mañana; configurable), arma la descripción con los
   números de crédito de las deudas y envía `singleUse: true` y `modifyAmount: false`.

   Respuesta: el QR emitido por Banco Económico (`qrId` y `qrImage`).

   Si el usuario desiste del pago, **`POST /api/BancoEconomico/annul-qr`** con
   `{"pagoCospailId": "…"}` anula el QR ante el banco y marca QR, pago y deudas
   como `Anulado`. Para pagar después (las mismas u otras deudas) basta iniciar
   un nuevo pago con **initiate**; el intento anulado queda como auditoría.

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

> `generate-qr` siempre requiere un `pagoCospailId` válido: el QR se genera únicamente a partir de un pago iniciado con **initiate**.

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
$env:ExternalServices__BanEcoApi__QrValidityHours = '0'
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
| POST | `/api/Cospail/payments/initiate` | Valida y persiste un pago agrupado de una o más deudas (rechaza si el socio tiene un QR activo). |
| GET | `/api/Cospail/payments/{pagoCospailId}` | Consulta el estado de un pago agrupado y sus deudas. |
| GET | `/api/Cospail/payments/active-qr` | Devuelve el QR vigente del socio (`fixedCode` + `documentId`) o `404` si no tiene ninguno. |
| POST | `/api/BancoEconomico/generate-qr` | Genera el QR de cobro de un pago (`pagoCospailId` + `branchCode`); importe, moneda, vencimiento y transacción se resuelven en la API. |
| POST | `/api/BancoEconomico/annul-qr` | Anula el QR vigente de un pago ante Banco Económico; QR, pago y deudas quedan `Anulado`. |
| POST | `/api/Cospail/payments/confirm` | Valida y registra un cobro individual en COSPAIL. |
| POST | `/api/qrsimple/notifyPaymentQR` | Callback de pago de Banco Económico. Marca el pago `Pagado` y registra los cobros en COSPAIL. |
| GET | `/health` | Health check (incluye conectividad con la base de datos). |

Los errores globales se entregan como `application/problem+json`: 400 para argumentos inválidos, 404 para recursos no encontrados y 500 para errores no controlados. El callback QR es la excepción: siempre devuelve 200 y utiliza `responseCode` (`0`, `1` o `99`).

En el callback, `payment.paymentDate` admite `yyyy-MM-dd` o `yyyy-MM-ddTHH:mm:ss` y `payment.branchCode` es opcional.



