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

- **Api** contiene los controladores `BancoEconomicoController`, `CospailController`, `NotifyPaymentQrController`, `AuthController` y `AdminController`, además del middleware de errores global.
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
8. Pagos recientes: `GET /api/Cospail/payments/recent` devuelve los últimos 5 pagos de un socio con sus deudas anidadas, filtrados por código fijo y estado (predeterminado: `CospailRegistrado`). Cada item contiene el `pagoCospailId` (seleccionable), el `totalAmount` y la lista de deudas con `creditNumber`, `period` y `amount`.

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

### Consulta de pagos recientes (maestro-detalle)

**`GET /api/Cospail/payments/recent?fixedCode=123&status=CospailRegistrado`** — devuelve los últimos 5 pagos del socio con sus deudas anidadas:

```json
[
  {
    "pagoCospailId": "3fa85f64-...",
    "totalAmount": 250.00,
    "debts": [
      { "creditNumber": 456, "period": "2026-01", "amount": 150.00 },
      { "creditNumber": 789, "period": "2026-02", "amount": 100.00 }
    ]
  },
  {
    "pagoCospailId": "a1b2c3d4-...",
    "totalAmount": 100.00,
    "debts": [
      { "creditNumber": 321, "period": "2026-03", "amount": 100.00 }
    ]
  }
]
```

El parámetro `status` es opcional y predeterminado a `CospailRegistrado`. La respuesta incluye las deudas de cada pago, lista para mostrar en una vista maestro-detalle en el frontend.

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
dotnet user-secrets set "Auth:SecretKey" "CLAVE_ALEATORIA_LARGA" --project src/Api
dotnet user-secrets set "Auth:Users:0:Username" "admin" --project src/Api
dotnet user-secrets set "Auth:Users:0:PasswordHash" "PBKDF2$100000$saltB64$hashB64" --project src/Api
dotnet user-secrets set "Auth:Users:0:DisplayName" "Administrador" --project src/Api
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

## Panel de administración

Los endpoints `/api/admin/*` exponen reportes para un sitio de administración (React/Vite/Tailwind) dedicado a revisar los pagos de Cospail y el detalle de sus deudas. Todos los endpoints de administración requieren un token JWT con el rol `Admin`.

### Autenticación

Configuración en la sección `Auth`. Se recomienda definir `SecretKey` y los usuarios mediante Secret Manager (desarrollo) o variables de entorno (producción); en `appsettings.Development.json` hay un usuario por defecto `admin` (password `admin123`).

```json
"Auth": {
  "Issuer": "cospail-admin",
  "Audience": "cospail-payments-api",
  "SecretKey": "<clave aleatoria de al menos 32 bytes>",
  "TokenLifetimeMinutes": 120,
  "Users": [
    { "Username": "admin", "PasswordHash": "PBKDF2$iteraciones$saltBase64$hashBase64", "DisplayName": "Administrador" }
  ]
}
```

Variables de entorno equivalentes:

```powershell
$env:Auth__SecretKey = 'CLAVE_ALEATORIA_LARGA'
$env:Auth__Users__0__Username = 'admin'
$env:Auth__Users__0__PasswordHash = 'PBKDF2$...'
$env:Auth__Users__0__DisplayName = 'Administrador'
```

1. **Iniciar sesión**: `POST /api/admin/auth/login` con `{ "username": "admin", "password": "..." }`. Devuelve `{ token, expiresAt, displayName }`.
2. **Usar el token**: los endpoints `/api/admin/*` llevan `Authorization: Bearer <token>`.

> **Precedencia de configuración.** La clave se resuelve en este orden (gana la primera que exista): **variables de entorno** > **Secret Manager** > `appsettings.Development.json` > `appsettings.json`. Los Secret Manager **solo se cargan cuando el entorno es `Development`** (p. ej. con los perfiles `http`/`https`). Si lanzas la app con otro entorno, los secrets se ignoran. Por eso, para producción es más confiable usar la variable de entorno `Auth__SecretKey`.
>
> **Longitud de la clave.** `Auth:SecretKey` debe tener al menos **32 bytes (256 bits)** porque se firma con HS256. Si es más corta, el arranque falla con un mensaje claro (antes lanzaba `IDX10720: Unable to create KeyedHashAlgorithm ...'168' bits`). El error `401` en un endpoint `/api/admin/*` casi siempre significa que falta un token válido; genera uno con `/api/admin/auth/login` y envíalo como `Authorization: Bearer <token>`.

Los passwords nunca se guardan en claro: se almacenan como hash PBKDF2 (SHA-256) con salt aleatorio por usuario.

### Cambio de password

Dado que los usuarios viven en configuración (sin base de datos), el cambio de password es manual: se genera un nuevo hash con la herramienta incluida y se actualiza la configuración (Secret Manager / variable de entorno / appsettings):

```powershell
dotnet run --project tools/PasswordHashGen -- "NuevoPassword"
```

El comando imprime un hash con formato `PBKDF2$iteraciones$saltBase64$hashBase64` listo para pegar en `Auth:Users[*].PasswordHash`. Después de actualizar la configuración hay que reiniciar la aplicación.

### Reporte de pagos

**`GET /api/admin/payments/report`** — reporte paginado (maestro-detalle) de `pagos_cospail`:

| Parámetro | Tipo | Descripción |
| --- | --- | --- |
| `from` | DateTime | Fecha de inicio del rango (opcional, sobre `CreatedAtUtc`). |
| `to` | DateTime | Fecha de fin del rango (opcional, inclusiva hasta fin de día). |
| `status` | enum | Estado del pago. Predeterminado: `CospailRegistrado`. `Pendiente`, `QRGenerado`, `Pagado`, `CospailRegistrado`, `Anulado`. |
| `fixedCode` | int | Código fijo del socio (opcional). |
| `documentId` | string | Documento de identidad o NIT del socio (opcional, búsqueda parcial). |
| `page` | int | Página solicitada, desde 1. |
| `pageSize` | int | Tamaño de página (máx. 100). |

```json
{
  "page": 1, "pageSize": 20, "totalCount": 147, "pageCount": 8,
  "items": [
    {
      "pagoCospailId": "3fa85f64-...", "fixedCode": 123, "documentId": "CI123",
      "memberName": "Juan Perez", "totalAmount": 250.00, "status": "CospailRegistrado",
      "createdAtUtc": "2026-08-01T12:00:00Z", "updatedAtUtc": null,
      "debts": [
        { "creditNumber": 456, "type": 1, "noticeNumber": 1, "year": 2026, "month": 1,
          "period": "2026-01", "amount": 150.00, "status": "CospailRegistrado" }
      ]
    }
  ]
}
```

El default `CospailRegistrado` sirve para el seguimiento: permite detectar pagos que quedaron en `Pagado` (pagados por Banco Económico pero aún no registrados en COSPAIL) si algo falla durante el registro.

**`GET /api/admin/payments/{pagoCospailId}`** — detalle de un pago con sus deudas y la notificación de pago QR asociada (datos del ordenante, importe, moneda, fechas) para conciliación. Devuelve `404` si el pago no existe.

> Nota: la firma JWT usa el esquema `SymmetricSecurityKey` con `Auth:SecretKey`. En `/swagger` (entornos Development o `Swagger:Enabled=true`) el panel tiene un botón de autorización Bearer para probar los endpoints de administración.

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
| GET | `/api/Cospail/payments/recent` | Devuelve los últimos 5 pagos de un socio con sus deudas anidadas (maestro-detalle). |
| GET | `/api/Cospail/payments/active-qr` | Devuelve el QR vigente del socio (`fixedCode` + `documentId`) o `404` si no tiene ninguno. |
| POST | `/api/BancoEconomico/generate-qr` | Genera el QR de cobro de un pago (`pagoCospailId` + `branchCode`); importe, moneda, vencimiento y transacción se resuelven en la API. |
| POST | `/api/BancoEconomico/annul-qr` | Anula el QR vigente de un pago ante Banco Económico; QR, pago y deudas quedan `Anulado`. |
| POST | `/api/Cospail/payments/confirm` | Valida y registra un cobro individual en COSPAIL. |
| POST | `/api/qrsimple/notifyPaymentQR` | Callback de pago de Banco Económico. Marca el pago `Pagado` y registra los cobros en COSPAIL. |
| POST | `/api/admin/auth/login` | Inicia sesión en el panel de administración y devuelve un token JWT. |
| GET | `/api/admin/payments/report` | Reporte paginado de pagos con sus deudas (requiere rol Admin). Filtros por fecha inicio/fin, estado, código fijo y documento. |
| GET | `/api/admin/payments/{pagoCospailId}` | Detalle de un pago con sus deudas y la notificación de pago QR asociada (requiere rol Admin). |
| GET | `/health` | Health check (incluye conectividad con la base de datos). |

Los errores globales se entregan como `application/problem+json`: 400 para argumentos inválidos, 404 para recursos no encontrados y 500 para errores no controlados. El callback QR es la excepción: siempre devuelve 200 y utiliza `responseCode` (`0`, `1` o `99`).

En el callback, `payment.paymentDate` admite `yyyy-MM-dd` o `yyyy-MM-ddTHH:mm:ss` y `payment.branchCode` es opcional.



