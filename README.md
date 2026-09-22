# Cospail.Payments.Backend

Web API en **.NET 10** que centraliza la consulta y el registro de cobros de COSPAIL (SOAP) y la emisión de cobros QR a través de Banco Económico.

- Stack: ASP.NET Core + EF Core + PostgreSQL + HttpClient (SOAP manual / JSON).
- OpenAPI/Swagger en `/swagger` (modo Development o `Swagger:Enabled=true`).
- Health check en `/health` (incluye conectividad con la base de datos).

## Índice

1. [Requisitos](#1-requisitos)
2. [Ejecución rápida](#2-ejecución-rápida)
3. [Configuración](#3-configuración)
4. [Arquitectura](#4-arquitectura)
5. [Flujos principales](#5-flujos-principales)
6. [Referencia de endpoints](#6-referencia-de-endpoints)
7. [Panel de administración](#7-panel-de-administración)
8. [Errores y convenciones](#8-errores-y-convenciones)
9. [Troubleshooting](#9-troubleshooting)

## 1. Requisitos

- .NET SDK 10.0 o superior (`dotnet-ef` 10.x para migraciones).
- PostgreSQL 18.x (o compatible).
- Acceso de red + credenciales de COSPAIL (SOAP) y Banco Económico.

## 2. Ejecución rápida

```powershell
dotnet restore
dotnet build
dotnet ef database update --project src/Infrastructure --startup-project src/Api
dotnet run --project src/Api --launch-profile https
# http -> --launch-profile http
```

- Swagger: `/swagger` (Development).
- CORS (`FrontendPolicy` en `src/Api/Program.cs`): solo `http://localhost:5173` por defecto.

## 3. Configuración

Nunca versionar credenciales reales. Precedencia: **variables de entorno > Secret Manager (solo Development) > `appsettings.Development.json` > `appsettings.json`**.

### 3.1 Variables de entorno

ASP.NET Core reemplaza `:` por `__`:

```powershell
$env:ExternalServices__CospailSoap__BaseUrl = 'https://ws.cospail.com.bo/wstest/wsco.asmx'
$env:ExternalServices__CospailSoap__Login = 'USUARIO_COSPAIL'
$env:ExternalServices__CospailSoap__Password = 'PASSWORD_COSPAIL'

$env:ExternalServices__BanEcoApi__BaseUrl = 'https://apimktdesa.baneco.com.bo/ApiGateway/'
$env:ExternalServices__BanEcoApi__UserName = 'USUARIO_BANECO'
$env:ExternalServices__BanEcoApi__EncryptedPassword = 'PASSWORD_CIFRADO_ENTREGADO_POR_BANECO'
$env:ExternalServices__BanEcoApi__AccountCredit = 'CUENTA_CIFRADA_O_CONFIGURADA_POR_BANECO'
$env:ExternalServices__BanEcoApi__QrValidityHours = '0'  # 0 = vence hoy (hora Bolivia), 24 = mañana

$env:ConnectionStrings__PaymentsDatabase = 'Host=localhost;Port=5432;Database=cospail_payments;Username=postgres;Password=TU_PASSWORD'

$env:Auth__SecretKey = 'CLAVE_ALEATORIA_DE_AL_MENOS_32_BYTES'
$env:Auth__Users__0__Username = 'admin'
$env:Auth__Users__0__PasswordHash = 'PBKDF2$...'
$env:Auth__Users__0__DisplayName = 'Administrador'
```

Notas:

- `CospailSoap:Login/Password` se inyectan en el sobre SOAP de `grabarCobrosWEB` y `ObtenerCobrosFecha`.
- `AccountCredit` se resuelve siempre en el servidor (generación de QR y consulta de movimientos); el consumidor no elige la cuenta.

### 3.2 Secret Manager (desarrollo)

```powershell
dotnet user-secrets init --project src/Api
dotnet user-secrets set "ExternalServices:CospailSoap:BaseUrl" "https://ws.cospail.com.bo/wstest/wsco.asmx" --project src/Api
dotnet user-secrets set "ExternalServices:CospailSoap:Login" "USUARIO_COSPAIL" --project src/Api
dotnet user-secrets set "ExternalServices:CospailSoap:Password" "PASSWORD_COSPAIL" --project src/Api
dotnet user-secrets set "ExternalServices:BanEcoApi:BaseUrl" "https://apimktdesa.baneco.com.bo/ApiGateway/" --project src/Api
dotnet user-secrets set "ExternalServices:BanEcoApi:UserName" "USUARIO_BANECO" --project src/Api
dotnet user-secrets set "ExternalServices:BanEcoApi:EncryptedPassword" "PASSWORD_CIFRADO_BANECO" --project src/Api
dotnet user-secrets set "ExternalServices:BanEcoApi:AccountCredit" "CUENTA_BANECO" --project src/Api
dotnet user-secrets set "ConnectionStrings:PaymentsDatabase" "Host=...;Database=cospail_payments;..." --project src/Api
dotnet user-secrets set "Auth:SecretKey" "CLAVE_ALEATORIA_LARGA" --project src/Api
```

### 3.3 Producción en VPS (variables de entorno)

`appsettings.Production.json` solo lleva lo no-sensible que difiere (CORS del frontend real,
URLs base de prod, `Swagger:Enabled=false`). Los secretos **nunca** van en archivos: se definen
como variables de entorno del servicio (`:` → `__`). Sin ellas la API no arranca
(`ValidateOnStart` falla con `OptionsValidationException` indicando qué falta).

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Production'

$env:ConnectionStrings__PaymentsDatabase = 'Host=localhost;Port=5432;Database=cospail_payments;Username=cospail;Password=GENERADA_EN_EL_VPS'

$env:ExternalServices__CospailSoap__Login = 'USUARIO_COSPAIL_PROD'
$env:ExternalServices__CospailSoap__Password = 'PASSWORD_COSPAIL_PROD'

$env:ExternalServices__BanEcoApi__UserName = 'USUARIO_BANECO_PROD'
$env:ExternalServices__BanEcoApi__EncryptedPassword = 'PASSWORD_CIFRADO_BANECO_PROD'
$env:ExternalServices__BanEcoApi__AccountCredit = 'CUENTA_BANECO_PROD'
# Opcional (default 0 = vence hoy): $env:ExternalServices__BanEcoApi__QrValidityHours = '24'

$env:Auth__SecretKey = 'CLAVE_ALEATORIA_NUEVA_DE_AL_MENOS_32_BYTES_DISTINTA_A_DEV'
$env:Auth__Users__0__Username = 'admin'
$env:Auth__Users__0__PasswordHash = 'PBKDF2$...'  # generar con: dotnet run --project tools/PasswordHashGen -- "NuevoPassword"
$env:Auth__Users__0__DisplayName = 'Administrador'
```

Notas:

- Reemplaza las `BaseUrl` de `appsettings.Production.json` por las URLs reales de prod
  (hoy son placeholders `REEMPLAZAR-...`) y el origen de `Cors:AllowedOrigins` por tu dominio.
- Genera un `SecretKey` y un password distintos a los de dev.
- Con systemd usa `EnvironmentFile=/etc/cospail-payments.env` (permiso `600`); con Docker Compose,
  un bloque `environment:` o `env_file` fuera del repo.

### 3.4 Migraciones PostgreSQL

La app exige `ConnectionStrings:PaymentsDatabase` pero no migra sola:

```powershell
dotnet tool install --global dotnet-ef --version 10.*
dotnet ef database update --project src/Infrastructure --startup-project src/Api
dotnet ef migrations add NombreDeLaMigracion --project src/Infrastructure --startup-project src/Api --output-dir Migrations
```

### 3.5 Tests

```powershell
dotnet test tests/Payments.Tests/Payments.Tests.csproj
# Integración contra base aislada:
$env:PAYMENTS_TEST_CONNECTION_STRING = 'Host=localhost;Port=5432;Database=cospail_payments_test;Username=postgres;Password=TU_PASSWORD'
```

## 4. Arquitectura

Clean Architecture / DDD:

```text
Cliente HTTP / Banco Económico (callback)
              |
              v
        Api (Controllers + middleware global)
              |
              v
Application (servicios, DTOs, interfaces)
              |
              v
Infrastructure (HttpClient SOAP/JSON + EF Core)
       |                         |
       v                         v
SOAP COSPAIL              API Banco Económico
```

- **Api**: `CospailController`, `BancoEconomicoController`, `NotifyPaymentQrController`, `AuthController`, `AdminController`, `AnalyticsController`, middleware de errores.
- **Application**: casos de uso en `src/Application/Services`, contratos en `src/Application/DTOs`.
- **Infrastructure**: `CospailSoapClient` (SOAP manual, 30s timeout), `BancoEconomicoQrClient`, persistencia PostgreSQL (`PagoCospail`, `DeudaCospail`, `PagoQr`, `notificaciones_pago_qr`).
- Hora de negocio: Bolivia (UTC-04:00) vía `BoliviaTime`.

## 5. Flujos principales

### 5.1 Pago con QR

El frontend consume en este orden: `active-qr` → `member-debt-by-document` → `initiate` → `generate-qr` → polling `payments/{id}`.

1. `GET /api/Cospail/payments/active-qr?fixedCode=123&documentId=CI123` — `200` = hay QR vigente (mostrarlo + botón anular); `404` = continuar.
2. `GET /api/Cospail/member-debt-by-document?fixedCode=123&documentId=CI123` — deudas vigentes.
3. `POST /api/Cospail/payments/initiate` — valida contra COSPAIL, persiste `PagoCospail` en `Pendiente` (`400` si hay QR activo):

   ```json
   { "fixedCode": 123, "documentId": "CI123",
     "debts": [{ "creditNumber": 456, "type": 1, "amount": 100.00 }] }
   ```

4. `POST /api/BancoEconomico/generate-qr` — solo `{ "pagoCospailId": "…", "branchCode": "001" }`. La API calcula total, fija `BOB`, genera `transactionId` propio (32 hex, guardado en `pagos_qr.transaction_id`), define `dueDate` por `QrValidityHours`, arma la descripción y envía `singleUse: true, modifyAmount: false`. Responde `qrId` + `qrImage` (el `qrId` lo genera el banco y se guarda en `pagos_qr.qr_id`). Pago → `QRGenerado`.
5. Callback `POST /api/qrsimple/notifyPaymentQR` (Banco Económico) — valida por `payment.qrId`, marca pago/deudas `Pagado`, persiste la notificación en `notificaciones_pago_qr` y registra cada cobro en COSPAIL (`grabarCobrosWEB`). Todo OK → `PagoRegistrado`; falla alguno → queda `Pagado` (reintento/conciliación). Ver con `GET /api/Cospail/payments/{pagoCospailId}`.
6. `POST /api/BancoEconomico/annul-qr` (`{ "pagoCospailId": "…" }`) — anula ante el banco (`DELETE api/qrsimple/cancelQR`); QR, pago y deudas → `Anulado`. Para pagar luego, nuevo `initiate`.

> `generate-qr` requiere un `pagoCospailId` en `Pendiente`; no se genera QR directo desde deudas.

### 5.2 Facturas últimos 6 meses

1. `GET /api/Cospail/invoices/last-6-months?fixedCode=123` — el backend calcula `FechaDesde = hoy - 6 meses`, `FechaHasta = hoy` (hora Bolivia) y llama a `ObtenerCobrosFecha`. Devuelve las últimas 6 facturas (`creditNumber`, período, fecha, importe).
2. `GET /api/Cospail/invoices/{creditNumber}/pdf` — llama a `obtenerUnaFacturaPDFB64` y devuelve `{ creditNumber, fileName, contentType: "application/pdf", pdfBase64 }`. Vacío → `404`.

### 5.3 Pagos recientes (maestro-detalle)

`GET /api/Cospail/payments/recent?fixedCode=123&status=PagoRegistrado` — últimos 5 pagos con deudas anidadas (`pagoCospailId`, `totalAmount`, `debts: [{ creditNumber, period, amount }]`). `status` opcional, default `PagoRegistrado`.

## 6. Referencia de endpoints

### Cospail (socios, pagos, facturas)

| Método | Ruta | Propósito |
| --- | --- | --- |
| GET | `/api/Cospail/member-debt-by-document` | Deudas y estado del socio (`fixedCode`, `documentId`). |
| POST | `/api/Cospail/payments/initiate` | Valida y persiste pago agrupado (`Pendiente`). |
| GET | `/api/Cospail/payments/{pagoCospailId}` | Estado de un pago y sus deudas. |
| GET | `/api/Cospail/payments/active-qr` | QR vigente (`fixedCode` + `documentId`) o `404`. |
| GET | `/api/Cospail/payments/recent` | Últimos 5 pagos con deudas (`fixedCode`, `status?`). |
| POST | `/api/Cospail/payments/confirm` | Valida y registra un cobro individual en COSPAIL. |
| GET | `/api/Cospail/invoices/last-6-months` | Últimas 6 facturas del socio (`fixedCode`). |
| GET | `/api/Cospail/invoices/{creditNumber}/pdf` | PDF de la factura en Base64. |

### Banco Económico

| Método | Ruta | Propósito |
| --- | --- | --- |
| POST | `/api/BancoEconomico/generate-qr` | Genera QR desde `pagoCospailId` (+ `branchCode?`). |
| POST | `/api/BancoEconomico/annul-qr` | Anula el QR (`pagoCospailId`). |
| GET | `/api/BancoEconomico/qr-status/{qrId}` | Verifica el estado de un QR en el banco (7.4: `0` pendiente, `1` pagado, `9` anulado). |
| GET | `/api/BancoEconomico/paid-qr/{fecha}` | QR pagados en una fecha `yyyyMMdd` para conciliación (7.6). |
| POST | `/api/BancoEconomico/query-movements` | Movimientos de la cuenta configurada por período `{ startDate, endDate }` (`yyyy-MM-dd`) (8.1). |
| POST | `/api/qrsimple/notifyPaymentQR` | Callback del banco (`responseCode` 0/1/99, siempre HTTP 200). |

> **Nota de nomenclatura (8.1):** el documento del banco nombraba este servicio como `history`, pero por email el banco confirmó que el nombre correcto es **`queryMovements`** (`POST api/accounts/queryMovements`). En este backend se usa `queryMovements`.
>
> Los tres endpoints de consulta (`qr-status`, `paid-qr`, `query-movements`) son **proxy puro**: autentican contra el banco y devuelven su respuesta sin modificar la BD local. `query-movements` usa siempre la cuenta de configuración (`ExternalServices:BanEcoApi:AccountCredit`); el cliente solo envía el período.

### Analíticas

| Método | Ruta | Auth | Propósito |
| --- | --- | --- | --- |
| POST | `/api/analytics/visits` | Anónimo (rate-limit) | Contador diario de visitas (1 por sesión). |
| GET | `/api/admin/analytics/summary` | Admin | Ingresos + QR del día/mes/año y serie mensual (`year?`). |

### Administración

| Método | Ruta | Propósito |
| --- | --- | --- |
| POST | `/api/admin/auth/login` | Login `{ username, password }` → `{ token, expiresAt, displayName }`. Con rate-limit (10 req/min por IP, `429` al exceder). |
| GET | `/api/admin/payments/report` | Reporte paginado de `pagos_cospail` (filtros `from,to,status,fixedCode,documentId,page,pageSize`). |
| GET | `/api/admin/payments/{pagoCospailId}` | Detalle + notificación QR para conciliación. |

### Infra

| Método | Ruta | Propósito |
| --- | --- | --- |
| GET | `/health` | Liveness + conectividad DB (también `HEALTHCHECK` del Dockerfile). |

Logs: consola siempre; archivo `logs/api-*.log` solo en Development. En producción los
captura systemd/Docker vía stdout.

Ejemplo callback (`POST /api/qrsimple/notifyPaymentQR` → `200 { "responseCode": 0, "message": "" }`):

```json
{ "payment": {
  "qrId": "22113001016800000017", "transactionId": "tx-abc-123",
  "paymentDate": "2026-08-11", "paymentTime": "10:23:45",
  "currency": "BOB", "amount": 150.00, "senderBankCode": "1016",
  "senderName": "CLIENTE DE PRUEBA 1234567", "senderAccount": "******5691",
  "description": "Pago de deudas Cospail", "branchCode": "001" } }
```

`paymentDate` admite `yyyy-MM-dd` o `yyyy-MM-ddTHH:mm:ss`; `branchCode` opcional.

> **IDs de `pagos_qr`:** `id` (uuid interno, PK para FKs) + `transaction_id` (nuestro, 32 hex enviado en `generateQR`) + `qr_id` (lo genera el banco). El `transactionId` del callback (`notificaciones_pago_qr.transaction_id`, ej. `18459506`) es el del banco y **no coincide** con `pagos_qr.transaction_id`. Ver detalle en `docs/ER.md`.

## 7. Panel de administración

Requiere JWT con rol `Admin` (`Authorization: Bearer <token>`). En Swagger hay botón Bearer. `SecretKey` ≥ 32 bytes (HS256).

```json
"Auth": {
  "Issuer": "cospail-admin", "Audience": "cospail-payments-api",
  "SecretKey": "<32+ bytes>", "TokenLifetimeMinutes": 120,
  "Users": [{ "Username": "admin", "PasswordHash": "PBKDF2$iteraciones$saltBase64$hashBase64", "DisplayName": "Administrador" }]
}
```

### 7.1 Reporte de pagos

`GET /api/admin/payments/report?from=&to=&status=PagoRegistrado&fixedCode=&documentId=&page=1&pageSize=20`

El default `PagoRegistrado` permite detectar pagos varados en `Pagado` (cobrados por el banco, no registrados en COSPAIL).

### 7.2 Cambio de password

Usuarios en configuración (sin DB). Generar hash y actualizar config + reiniciar:

```powershell
dotnet run --project tools/PasswordHashGen -- "NuevoPassword"
```

## 8. Errores y convenciones

- Global `application/problem+json`: `400` argumentos/validación (`ValidationException`, `ArgumentException`), `404` no encontrado (`KeyNotFoundException`), `500` no controlado.
- Excepción: el callback QR siempre responde HTTP 200 con `responseCode` (`0` OK, `1` datos inválidos, `99` error interno).
- Logs SOAP truncan la respuesta externa (evita exponer PII).

## 9. Troubleshooting

| Síntoma | Causa probable | Acción |
| --- | --- | --- |
| `401` en `/api/admin/*` | Falta token o expirado | Login en `/api/admin/auth/login` y reenviar `Bearer`. |
| Falla al arrancar (`IDX10720` o clave) | `Auth:SecretKey` < 32 bytes | Generar clave ≥ 32 bytes (variables de entorno en prod). |
| Secrets ignorados | Entorno ≠ `Development` | Usar variables de entorno (prod) en vez de Secret Manager. |
| `Anulado` no deja pagar | Flujo correcto | Crear nuevo `initiate`; el intento anulado es auditoría. |
| Pago en `Pagado` no avanza | Falló `grabarCobrosWEB` | Revisar reporte admin + logs, conciliar/reintentar. |
