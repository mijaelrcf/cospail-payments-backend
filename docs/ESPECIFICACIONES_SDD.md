# Especificaciones reconstruidas — COSPAIL Payments Backend

> Estado: actualizado el 2026-08-10 para reflejar la implementación vigente
> (validación centralizada, contrato del callback QR, health check y
> eliminación del endpoint público de autenticación).
>
> Este documento trata el código, el contrato OpenAPI y los manuales de
> integración presentes en el repositorio como la fuente de verdad. No describe
> funcionalidad futura como si ya estuviera disponible.

## 1. Propósito y alcance

El sistema expone una API HTTP para:

1. consultar deudas de socios en el servicio SOAP de COSPAIL;
2. verificar una deuda contra el socio y su documento, y registrar su cobro en
   COSPAIL;
3. generar solicitudes de pago QR mediante Banco Económico; y
4. recibir y acusar las notificaciones de pago QR del banco.

Las especificaciones se identifican con `SPEC-xxx`. Los criterios de aceptación
están expresados como comportamiento observable y permiten convertir cada ítem
en pruebas automatizadas de aceptación.

## 2. Actores y sistemas externos

| Actor o sistema | Responsabilidad |
| --- | --- |
| Consumidor de la API / frontend | Consulta deudas, confirma cobros y solicita QR. |
| Banco Económico | Autentica la integración, genera QR y notifica pagos realizados. |
| SOAP COSPAIL | Devuelve deudas y registra cobros. |
| API de pagos | Orquesta los flujos y traduce errores al contrato HTTP. |

## 3. Especificaciones funcionales

### SPEC-001 — Consultar deuda por código fijo

**Objetivo.** Obtener la primera deuda devuelta por COSPAIL para un código fijo.

**Contrato.** `GET /api/CospailSoap/debt/{fixedCode}`.

**Reglas y criterios de aceptación.**

- Dado un `fixedCode` mayor a cero, cuando COSPAIL responde con una tabla de
  deuda, entonces se devuelve `200 OK` con código fijo, aviso, crédito, tipo,
  período, socio e importe disponibles.
- Dado un `fixedCode` menor o igual a cero, entonces se devuelve `400` con
  Problem Details de validación.
- Dado que COSPAIL no devuelve una tabla de deuda, entonces se devuelve `404`.
- La integración invoca la operación SOAP `ObtenerDeudaSocioCF` con el parámetro
  `liCfijo`.

### SPEC-002 — Consultar deudas y estado del socio por documento

**Objetivo.** Identificar el estado del socio y sus deudas mediante código fijo
y CI/NIT.

**Contrato.** `GET /api/CospailSoap/member-debt-by-document?fixedCode={n}&documentId={valor}`.

**Reglas y criterios de aceptación.**

- `fixedCode` es obligatorio y debe ser mayor que cero; `documentId` es
  obligatorio y no puede ser vacío. Las infracciones devuelven `400`.
- La integración invoca `ObtenerDeudaSocioDide` con `liCFijo` y `lsDide`.
- Si COSPAIL devuelve filas de deuda válidas, el resultado es `HasDebt`, incluye
  el nombre del socio y la lista completa de deudas.
- Si no hay filas, o COSPAIL informa `SIN DEUDA` con importe cero, el resultado
  es `NoDebt` y la lista queda vacía.
- Si COSPAIL informa que el socio no existe (importe `-1` o nombre que contiene
  `NO EXISTE`), el resultado es `MemberNotFound` y la lista queda vacía.
- Si COSPAIL informa `NO COINCIDE CI/NIT` con importe cero, el resultado es
  `DocumentMismatch` y la lista queda vacía.

### SPEC-003 — Confirmar y registrar un cobro COSPAIL

**Objetivo.** Evitar registrar cobros para un socio, documento o deuda que no
coincidan con la información vigente de COSPAIL.

**Contrato.** `POST /api/CospailSoap/payments/confirm` con `fixedCode`,
`documentId`, `creditNumber`, `type` y `amount`.

**Reglas y criterios de aceptación.**

- Antes de registrar, el sistema consulta la deuda por código fijo y documento.
- Si el estado es `MemberNotFound`, `DocumentMismatch` o `NoDebt`, el proceso
  termina sin registrar el cobro y devuelve un error `500` con el mensaje
  genérico del manejador global actual.
- Solo se registra el cobro si existe una deuda con el mismo `creditNumber`,
  `type` e `amount` exacto. De lo contrario, no se registra y se devuelve `500`.
- Para una deuda coincidente, se invoca `grabarCobrosWEB` con crédito, tipo,
  importe a dos decimales, fecha/hora local de Bolivia (UTC-04:00) y las
  credenciales configuradas de COSPAIL.
- Una respuesta SOAP cruda igual a `"1"` produce `success: true` y el mensaje
  `Cobro registrado correctamente.`; cualquier otro valor produce
  `success: false` y conserva el valor de la respuesta SOAP como mensaje.
- La respuesta exitosa conserva los datos de la solicitud y añade el nombre del
  socio. El resultado crudo de COSPAIL no se expone en la respuesta pública.

### SPEC-004 — Generar un QR de cobro

**Objetivo.** Crear un QR de Banco Económico para un importe, moneda y fecha de
vencimiento indicados por el consumidor.

**Contrato.** `POST /api/BancoEconomico/generate-qr`.

**Reglas y criterios de aceptación.**

- Antes de solicitar el QR, el sistema se autentica ante Banco Económico.
- El token obtenido se utiliza como cabecera `Authorization: Bearer` para
  `POST api/qrsimple/generateQR`.
- La cuenta de abono enviada por el consumidor se reemplaza siempre por
  `ExternalServices:BanEcoApi:AccountCredit`; por tanto, el consumidor no puede
  elegir la cuenta destino.
- `transactionId` es obligatorio (máx. 100 caracteres) y `description` tiene un
  límite de 500 caracteres; exceder esos límites devuelve `400`.
- Un `transactionId` ya registrado devuelve `400`, incluso cuando una petición
  concurrente intenta registrar el mismo identificador.
- Si el banco responde HTTP exitoso y `responseCode: 0`, se devuelve el QR,
  incluyendo `qrId` y, cuando el banco lo entregue, `qrImage`.
- Tras una emisión exitosa, se registra el QR con estado `Pendiente`, fecha y
  hora UTC de creación, e identificadores únicos `transactionId` y `qrId`.
- Errores HTTP, errores de deserialización y códigos funcionales distintos de
  cero se devuelven como `500`.

### SPEC-005 — Recibir notificación de pago QR

**Objetivo.** Validar y acusar las notificaciones de pago enviadas por Banco
Económico.

**Contrato.** `POST /api/qrsimple/notifyPaymentQR` con el objeto `payment`.

**Reglas y criterios de aceptación.**

- El endpoint siempre responde `200 OK`; el resultado funcional se comunica en
  `responseCode`.
- Son obligatorios: `qrId`, `transactionId`, fecha, hora, moneda, importe,
  código de banco emisor, nombre, cuenta de origen y descripción. La sucursal
  (`branchCode`) es opcional.
- `payment.paymentDate` admite `yyyy-MM-dd` o `yyyy-MM-ddTHH:mm:ss`; la hora
  debe tener formato `HH:mm:ss` y el importe debe ser mayor que cero.
- La moneda se normaliza a mayúsculas y debe ser `BOB` o `USD`.
- Una entrada inválida devuelve `responseCode: 1` y explica el campo inválido.
- Un error no controlado devuelve `responseCode: 99` y un mensaje genérico.
- Una entrada válida devuelve `responseCode: 0` y mensaje vacío.
- `senderDocumentId` se recibe pero no se valida ni se utiliza actualmente.
- Para un QR pendiente cuyo `transactionId` coincide, la notificación lo marca
  como `Pagado` y conserva la fecha/hora de pago en UTC. Las repeticiones de
  una notificación ya procesada son idempotentes.
- Cada notificación válida y acusada con `responseCode: 0` se persiste en la
  tabla `notificaciones_pago_qr` con una instantánea del pago: `qrId`,
  `transactionId`, fecha y hora de pago (en UTC e informadas por el banco),
  moneda, importe, `senderBankCode`, `senderName`, `senderDocumentId`,
  `senderAccount`, `description`, `branchCode` y la fecha/hora UTC de recepción;
  los reintentos del banco quedan registrados como nuevas filas.
- Un QR inexistente o un `transactionId` no coincidente devuelve `responseCode: 1`.
- Un importe distinto al solicitado (salvo que el QR permita `modifyAmount`) o
  una moneda distinta a la del QR devuelven `responseCode: 1`.

### SPEC-006 — Verificar disponibilidad de la API

**Objetivo.** Permitir una comprobación de que el proceso HTTP y la base de
datos están disponibles.

**Contrato.** `GET /health`.

**Criterios de aceptación.**

- Con la base de datos accesible, devuelve `200` con el estado `Healthy`.
- Con la base de datos inaccesible, devuelve `503` con el estado `Unhealthy`.

## 4. Requisitos no funcionales y restricciones actuales

| ID | Especificación |
| --- | --- |
| NFR-001 | La API redirige a HTTPS y aplica CORS únicamente a `http://localhost:5173`. |
| NFR-002 | En desarrollo publica Swagger en `/swagger`. |
| NFR-003 | Los errores no controlados se registran y devuelven `application/problem+json` con HTTP 500; `ValidationException` y `ArgumentException` se convierten en 400 y `KeyNotFoundException` en 404. |
| NFR-004 | Los clientes externos usan `HttpClient`; tanto el cliente de COSPAIL como el de Banco Económico tienen 30 segundos de timeout. |
| NFR-005 | Las credenciales y las URLs de proveedores se obtienen desde `ExternalServices:CospailSoap` y `ExternalServices:BanEcoApi`. |
| NFR-006 | Se registra información de trazabilidad de QR (transacción, QR, importe y datos del ordenante) en la tabla `notificaciones_pago_qr` y mediante el sistema de logging. |
| NFR-007 | Los logs de error del cliente SOAP truncan la respuesta externa para evitar exponer datos personales. |

## 5. Fuera de alcance / trabajo pendiente explícito

- **SPEC-FUT-001 — Confirmación automática tras callback QR.** El callback
  válido solo se valida, registra y acusa. No llama a `grabarCobrosWEB`.
- **SPEC-FUT-003 — Conciliación QR.** No se relaciona actualmente
  `transactionId`/`qrId` con una deuda COSPAIL antes de acreditar el pago.
- **SPEC-FUT-004 — Verificación del pago contra el banco.** El callback se
  acepta tal cual; no se consultan los servicios `statusQR`/`paidQR` del banco
  para confirmar el pago, ni existe corrección manual de pagos.

## 6. Trazabilidad de implementación

| Especificación | Componentes principales |
| --- | --- |
| SPEC-001 y SPEC-002 | `CospailSoapController`, `CospailSoapService`, `CospailSoapClient` |
| SPEC-003 | `CospailSoapController`, `CospailSoapService`, `CospailSoapClient.RecordPaymentAsync` |
| SPEC-004 | `BancoEconomicoController`, `BancoEconomicoService`, `BancoEconomicoQrClient` |
| SPEC-005 | `NotifyPaymentQrController`, `BancoEconomicoService.HandlePaymentNotificationAsync` |
| SPEC-006 | `Program` (`MapHealthChecks` + `AddDbContextCheck`) |

## 7. Observaciones de consistencia para un proceso SDD futuro

1. El contrato OpenAPI generado no documenta `GET /health` (los health checks
   de ASP.NET Core no se incluyen en Swagger por defecto).
2. El contrato publica respuestas `400` y `404` para la confirmación de pagos,
   pero la implementación arroja `InvalidOperationException` en sus reglas de
   negocio; el middleware actual lo expone como `500`.
3. La especificación OpenAPI exige `accountCredit` en la solicitud de QR, pero
   la implementación lo ignora y lo reemplaza por configuración. Es conveniente
   convertirlo en opcional o eliminarlo del contrato público.
4. El proyecto `Payments.Tests` cubre con pruebas automatizadas los validadores,
   los servicios de aplicación, los controladores y el modelo de datos. Los
   criterios de aceptación que dependen de los servicios externos (SOAP COSPAIL
   y API de Banco Económico) requieren verificación manual contra los entornos
   de prueba.
