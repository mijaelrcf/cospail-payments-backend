# API Market - Banco Económico S.A.
## Especificaciones Técnicas v1.3.0

**Documento:** API Market Especificaciones Técnicas v1.3.0  
**Organización:** BANCO ECONÓMICO S.A. | Tecnología de Información  
**Última actualización:** 28/04/2025

---

## Tabla de Contenidos

1. [Introducción](#introducción)
2. [Formatos y Convenciones](#formatos-y-convenciones)
3. [Encriptación de Datos](#encriptación-de-datos)
4. [URL de Ambiente de Certificación](#url-de-ambiente-de-certificación)
5. [API de Encriptación](#api-de-encriptación)
6. [API de Autenticación](#api-de-autenticación)
7. [API de Pagos Simple con Códigos QR](#api-de-pagos-simple-con-códigos-qr)
8. [Consultas de Cuentas](#consultas-de-cuentas)
9. [Planillas de Pagos](#planillas-de-pagos)
10. [Anexo 1 - Definiciones de Objetos](#anexo-1--definiciones-de-objetos)

---

## Introducción

El presente documento describe las especificaciones técnicas de las APIs que el Banco Económico S.A. pone a disposición de sus clientes.

### Características Principales

- **Tecnología:** REST
- **Formato de Intercambio:** JSON
- **Autenticación:** Bearer Token (generado mediante API de autenticación)
- **Seguridad:** Encriptación AES 256 bits

### Flujo de Autenticación

Para poder hacer uso de cualquier API, se debe:

1. Enviar credenciales a través de la API de autenticación
2. Obtener un Bearer Token
3. Incluir el token en la cabecera de las solicitudes posteriores

---

## Formatos y Convenciones

### Importes con Decimales

- Separador: punto (`.`)
- Máximo: dos dígitos para la parte decimal
- Ejemplo: `100.50`

### Fechas

- Formato: `yyyy-MM-dd`
- Ejemplo: `2025-04-28`

### Horas

- Formato: `HH:mm:ss` (formato 24 horas)
- Ejemplo: `15:30:45`

### Nombres de Propiedades

- Notación: **camelCase**
- Ejemplo: `accountCode`, `paymentDate`, `senderBankCode`

---

## Encriptación de Datos

### Algoritmo

- **Tipo:** AES (Advanced Encryption Standard)
- **Clave:** 256 bits (32 bytes)
- **Distribución:** Proporcionada por el banco para ambientes de certificación y producción

La encriptación se utiliza para datos sensibles que se envían o reciben a través de las diferentes APIs.

---

## URL de Ambiente de Certificación

```
https://apimktdesa.baneco.com.bo/ApiGateway/
```

---

## API de Encriptación

### 5.1 Encriptar Datos

Realiza encriptación AES de texto utilizando una clave proporcionada.

| Propiedad | Valor |
|-----------|-------|
| **Descripción** | Encriptación de datos |
| **Método** | GET |
| **URI** | `http://[dominio]:[puerto]/api/authentication/encrypt` |

#### Parámetros

| Elemento | Tipo de Dato | Requerido | Descripción |
|----------|--------------|-----------|-------------|
| `text` | Texto | Sí | Texto a encriptar |
| `aesKey` | Texto | Sí | Llave de encriptación |

#### Ejemplo de Solicitud

```http
https://apimktdesa.bancavive.com.bo/ApiGateway/api/authentication/encrypt?text=1234&aesKey=40A318B299F245C2B697176723088629
```

#### Ejemplo de Respuesta

```
KJAzqjmwjxIOqVo5J3IH0/7fGmNdzuyszrlqexVSeos=
```

---

### 5.2 Desencriptar Datos

Realiza desencriptación AES de texto encriptado.

| Propiedad | Valor |
|-----------|-------|
| **Descripción** | Desencriptación de datos |
| **Método** | GET |
| **URI** | `http://[dominio]:[puerto]/api/authentication/decrypt` |

#### Parámetros

| Elemento | Tipo de Dato | Requerido | Descripción |
|----------|--------------|-----------|-------------|
| `text` | Texto | Sí | Texto a desencriptar |
| `aesKey` | Texto | Sí | Llave de encriptación |

#### Ejemplo de Solicitud

```http
https://apimktdesa.bancavive.com.bo/ApiGateway/api/authentication/decrypt?text=KJAzqjmwjxIOqVo5J3IH0/7fGmNdzuyszrlqexVSeos=&aesKey=40A318B299F245C2B697176723088629
```

#### Ejemplo de Respuesta

```
1234
```

---

## API de Autenticación

### 6.1 Validación de Credenciales de Acceso

Obtiene un token de autorización para consumir otros servicios.

| Propiedad | Valor |
|-----------|-------|
| **Descripción** | Validación de credenciales y solicitud de token |
| **Método** | POST |
| **URI** | `http://[dominio]:[puerto]/api/authentication/authenticate` |

#### Body de la Solicitud

| Elemento | Tipo de Dato | Requerido | Descripción |
|----------|--------------|-----------|-------------|
| `userName` | Texto | Sí | Nombre de usuario asignado por el Banco |
| `password` | Texto | Sí | Contraseña encriptada |

#### Respuesta

| Elemento | Tipo de Dato | Descripción |
|----------|--------------|-------------|
| `responseCode` | Entero | Código de respuesta; diferente de cero indica error |
| `message` | Texto | Mensaje de error (si `responseCode` ≠ 0) |
| `token` | Texto | Token de autorización para otros servicios |

#### Ejemplo de Solicitud

```json
{
  "userName": "26551010",
  "password": "gmcqdMrrZsg1k7BZPgHC+95EINE073qdT8llUklDEcM="
}
```

#### Ejemplo de Respuesta

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c3IiOiIyNjU1MTAxMCIsIm5iZiI6MTYyMzQ0NzU1OC...",
  "responseCode": 0,
  "message": ""
}
```

---

## API de Pagos Simple con Códigos QR

### Diagrama de Secuencia

El flujo de pago con QR involucra:

1. **Cliente** solicita pagar con código QR al comercio
2. **Comercio** solicita token de seguridad al banco
3. **Comercio** solicita generación de código QR al banco
4. **Banco Económico** proporciona código QR
5. **Cliente** realiza pago con app de su entidad financiera
6. **Banco Económico** recibe el pago
7. **Banco Económico** notifica pago al comercio (opcional)
8. **Comercio** confirma recepción de pago al cliente

---

### 7.2 Generación de QR

Solicita la generación de un código QR para cobros a través de la plataforma Pago Simple.

| Propiedad | Valor |
|-----------|-------|
| **Descripción** | Solicitud de generación de código QR |
| **Método** | POST |
| **URI** | `http://[dominio]:[puerto]/api/qrsimple/generateQR` |

#### Body de la Solicitud

| Elemento | Tipo de Dato | Requerido | Descripción |
|----------|--------------|-----------|-------------|
| `transactionId` | Texto | Sí | Identificador de la transacción en el sistema del comercio |
| `accountCredit` | Texto | Sí | Número de cuenta corriente o caja de ahorro a acreditar (encriptado) |
| `currency` | Texto | Sí | Moneda: `BOB` (bolivianos) o `USD` (dólares americanos) |
| `amount` | Decimal | Sí | Importe del QR (máximo 2 decimales) |
| `description` | Texto | No | Nota del cobro |
| `dueDate` | Fecha | Sí | Fecha de vencimiento del QR |
| `singleUse` | Lógico | Sí | `true`: solo permite 1 pago; `false`: permite varios pagos |
| `modifyAmount` | Lógico | Sí | `true`: permite modificar importe; `false`: rechaza pago con importe diferente |
| `branchCode` | Texto | No | Código de sucursal (máximo 5 caracteres) |

#### Respuesta

| Elemento | Tipo de Dato | Descripción |
|----------|--------------|-------------|
| `responseCode` | Entero | Código de respuesta; diferente de cero indica error |
| `message` | Texto | Mensaje de error (si `responseCode` ≠ 0) |
| `qrId` | Texto | Identificador único del QR |
| `qrImage` | Texto | Imagen del código QR en formato base64 |

#### Ejemplo de Solicitud

```json
{
  "transactionId": "123456789",
  "accountCredit": "y6G5mb6P1UVMsGR+2mdEaZ0970Gyg6eSt3SxOaizwIY=",
  "currency": "BOB",
  "amount": 1.2,
  "description": "Ejemplo generacion de QR",
  "dueDate": "2021-12-31",
  "singleUse": true,
  "modifyAmount": false,
  "branchCode": "E0001"
}
```

#### Ejemplo de Respuesta

```json
{
  "qrId": "21061401016000000003",
  "qrImage": "iVBORw0KGgoAAAANSUhEUgAAB5QAAAeUCAYAAACZoCvZA......",
  "responseCode": 0,
  "message": ""
}
```

---

### 7.3 Anular QR

Anula un QR para futuros pagos.

| Propiedad | Valor |
|-----------|-------|
| **Descripción** | Anula un QR de uso único (no pagado) o un QR de uso múltiple |
| **Método** | DELETE |
| **URI** | `http://[dominio]:[puerto]/api/qrsimple/cancelQR` |

#### Body de la Solicitud

| Elemento | Tipo de Dato | Requerido | Descripción |
|----------|--------------|-----------|-------------|
| `qrId` | Texto | Sí | Identificador único del QR |

#### Respuesta

| Elemento | Tipo de Dato | Descripción |
|----------|--------------|-------------|
| `responseCode` | Entero | Código de respuesta; diferente de cero indica error |
| `message` | Texto | Mensaje de error (si `responseCode` ≠ 0) |

#### Ejemplo de Solicitud

```json
{
  "qrId": "21061401016000000003"
}
```

#### Ejemplo de Respuesta

```json
{
  "responseCode": 0,
  "message": ""
}
```

---

### 7.4 Verificar Estado de QR

Consulta el estado actual de un código QR.

| Propiedad | Valor |
|-----------|-------|
| **Descripción** | Consulta el estado de un código QR |
| **Método** | GET |
| **URI** | `http://[dominio]:[puerto]/api/qrsimple/v2/statusQR/{id}` |

#### Parámetros de Ruta

| Elemento | Tipo de Dato | Descripción |
|----------|--------------|-------------|
| `id` | Texto | Identificador único del QR (qrId) |

#### Respuesta

| Elemento | Tipo de Dato | Descripción |
|----------|--------------|-------------|
| `responseCode` | Entero | Código de respuesta; diferente de cero indica error |
| `message` | Texto | Mensaje de error (si `responseCode` ≠ 0) |
| `statusQRCode` | Entero | Código del estado: `0` (activo pendiente), `1` (pagado), `9` (anulado) |
| `payment` | PaymentQR[] | Información de transacciones de pago (cuando `statusQRCode = 1`) |

#### Ejemplo de Solicitud

```http
https://apimktdesa.baneco.com.bo/ApiGateway/api/qrsimple/v2/statusQR/21061401016000000006
```

#### Ejemplo de Respuesta

```json
{
  "statusQrCode": 1,
  "payment": [
    {
      "qrId": "21061401016000000006",
      "transactionId": "1236342",
      "paymentDate": "2021-06-14T00:00:00",
      "paymentTime": "17:06:29",
      "currency": "BOB",
      "amount": 1,
      "senderBankCode": "1016",
      "senderName": "PEDRO PEREZ",
      "senderDocumentId": "0",
      "senderAccount": "******1913",
      "description": "Ejemplo generacion de QR",
      "branchCode": "E0001"
    }
  ],
  "responseCode": 0,
  "message": ""
}
```

---

### 7.5 Notificación de Pago de QR (Opcional)

Servicio publicado por el comercio para recibir una notificación al momento del pago.

| Propiedad | Valor |
|-----------|-------|
| **Descripción** | Notificación de pago de QR |
| **Método** | POST |
| **URI** | `http://[dominio]:[puerto]/api/qrsimple/notifyPaymentQR` |

#### Body de la Solicitud

| Elemento | Tipo de Dato | Requerido | Descripción |
|----------|--------------|-----------|-------------|
| `payment` | PaymentQR | Sí | Objeto con información de la transacción |

#### Respuesta

| Elemento | Tipo de Dato | Descripción |
|----------|--------------|-------------|
| `responseCode` | Entero | Código de respuesta; diferente de cero indica error |
| `message` | Texto | Mensaje de error (si `responseCode` ≠ 0) |

#### Ejemplo de Solicitud

```json
{
  "payment": {
    "qrId": "22113001016800000017",
    "transactionId": "3161056",
    "paymentDate": "2022-11-30T00:00:00",
    "paymentTime": "15:00:27",
    "currency": "USD",
    "amount": 1.2,
    "senderBankCode": "1016",
    "senderName": "NOMBRECLIENTE 409182",
    "senderDocumentId": "0",
    "senderAccount": "******5691",
    "description": "Ejemplo generacion de QR"
  }
}
```

#### Ejemplo de Respuesta

```json
{
  "responseCode": 0,
  "message": ""
}
```

---

### 7.6 Lista de QR Pagados

Retorna el listado de QR pagados en una fecha específica, para uso en procesos de conciliación.

| Propiedad | Valor |
|-----------|-------|
| **Descripción** | Retorna QR pagados en una fecha |
| **Método** | GET |
| **URI** | `http://[dominio]:[puerto]/api/qrsimple/v2/paidQR/{fecha}` |

#### Parámetros de Ruta

| Elemento | Tipo de Dato | Descripción |
|----------|--------------|-------------|
| `fecha` | Texto | Fecha en formato `yyyyMMdd` |

#### Respuesta

| Elemento | Tipo de Dato | Descripción |
|----------|--------------|-------------|
| `responseCode` | Entero | Código de respuesta; diferente de cero indica error |
| `message` | Texto | Mensaje de error (si `responseCode` ≠ 0) |
| `paymentList` | PaymentQR[] | Lista de objetos PaymentQR con pagos recibidos |

#### Ejemplo de Solicitud

```http
https://apimktdesa.baneco.com.bo/ApiGateway/api/qrsimple/v2/paidQR/20210719
```

#### Ejemplo de Respuesta

```json
{
  "paymentList": [
    {
      "qrId": "21070201016000000006",
      "transactionId": "1236392",
      "paymentDate": "2021-07-19T00:00:00",
      "paymentTime": "13:34:28",
      "currency": "BOB",
      "amount": 2.5,
      "senderBankCode": "1016",
      "senderName": "APE1-101434 APE2-101434 NOMB-101434",
      "senderDocumentId": "0",
      "senderAccount": "******1913",
      "description": "Ejemplo generacion de QR",
      "branchCode": "E0001"
    },
    {
      "qrId": "21071401016000000001",
      "transactionId": "1236394",
      "paymentDate": "2021-07-19T00:00:00",
      "paymentTime": "15:05:46",
      "currency": "BOB",
      "amount": 1.2,
      "senderBankCode": "1016",
      "senderName": "APE1-101434 APE2-101434 NOMB-101434",
      "senderDocumentId": "0",
      "senderAccount": "******1913",
      "description": "Ejemplo generacion de QR",
      "branchCode": "E0002"
    }
  ],
  "responseCode": 0,
  "message": ""
}
```

---

## Consultas de Cuentas

### 8.1 Consulta de Movimientos

Obtiene los movimientos de una cuenta corriente o caja de ahorros por período.

| Propiedad | Valor |
|-----------|-------|
| **Descripción** | Consulta de movimientos por período |
| **Método** | POST |
| **URI** | `http://[dominio]:[puerto]/api/accounts/history` |

#### Body de la Solicitud

| Elemento | Tipo de Dato | Requerido | Descripción |
|----------|--------------|-----------|-------------|
| `accountCode` | Texto | Sí | Número de cuenta (encriptado) |
| `startDate` | Fecha | Sí | Fecha de inicio en formato `yyyy-MM-dd` |
| `endDate` | Fecha | Sí | Fecha final en formato `yyyy-MM-dd` |

#### Respuesta

| Elemento | Tipo de Dato | Descripción |
|----------|--------------|-------------|
| `responseCode` | Entero | Código de respuesta; diferente de cero indica error |
| `message` | Texto | Mensaje de error (si `responseCode` ≠ 0) |
| `accountHeader` | AccountHeader | Información de la cuenta |
| `accountDetailList` | AccountDetail[] | Lista de movimientos |
| `accountWithheldList` | AccountWithheld[] | Lista de retenciones |

#### Ejemplo de Solicitud

```json
{
  "accountCode": "y6G5mb6P1UVMsGR+2mdEaZ0970Gyg6eSt3SxOaizwIY=",
  "startDate": "2025-01-15",
  "endDate": "2025-01-20"
}
```

#### Ejemplo de Respuesta

```json
{
  "responseCode": 0,
  "message": "",
  "accountHeader": {},
  "accountDetailList": [],
  "accountWithheldList": []
}
```

---

## Planillas de Pagos

### 9.1 Carga de Planilla de Pagos

Carga una planilla de pagos a proveedores o de sueldos para procesamiento.

| Propiedad | Valor |
|-----------|-------|
| **Descripción** | Carga de planillas de pagos |
| **Método** | POST |
| **URI** | `http://[dominio]:[puerto]/api/batchPayment/upload` |

#### Body de la Solicitud

| Elemento | Tipo de Dato | Requerido | Descripción |
|----------|--------------|-----------|-------------|
| `batchId` | Texto | Sí | Identificador único de la planilla |
| `type` | Texto | Sí | Tipo: `PAYROLL` (sueldos) o `PROVIDERS` (proveedores) |
| `description` | Texto | Sí | Descripción o motivo del pago |
| `detailedDebit` | Lógico | Sí | `true`: débito por cada ítem; `false`: débito por total |
| `accountCode` | Texto | Sí | Número de cuenta a debitar |
| `batchCurrency` | Texto | Sí | Moneda: `BOB` o `USD` |
| `batchAmount` | Decimal | Sí | Importe total (máximo 2 decimales) |
| `AMLData` | AMLData | Sí | Información sobre origen y destino de fondos |
| `paymentCount` | Entero | Sí | Cantidad de pagos en la planilla |
| `paymentList` | BatchPayment[] | Sí | Detalle de pagos |

#### Respuesta

| Elemento | Tipo de Dato | Descripción |
|----------|--------------|-------------|
| `responseCode` | Entero | Código de respuesta; diferente de cero indica error |
| `message` | Texto | Mensaje de error (si `responseCode` ≠ 0) |
| `bankBatchId` | Entero | Número de planilla asignado por el banco |

---

### 9.2 Confirmación de Estado de Detalles de Planilla

Servicio publicado por la empresa para recibir confirmación del estado de cada pago.

| Propiedad | Valor |
|-----------|-------|
| **Descripción** | Confirmación de estado de detalles |
| **Método** | POST |
| **URI** | `http://[dominio]:[puerto]/api/notifyStatus` |

#### Body de la Solicitud

| Elemento | Tipo de Dato | Requerido | Descripción |
|----------|--------------|-----------|-------------|
| `bankBatchId` | Entero | Sí | Número de planilla asignado por el banco |
| `batchId` | Texto | Sí | Identificador único de la planilla |
| `batchDetailId` | Texto | Sí | Identificador único del pago |
| `status` | Texto | Sí | Estado: `ACEP` (aceptado) o `RECH` (rechazado) |
| `descriptionStatus` | Texto | No | Descripción o motivo del rechazo |
| `transactionIdDebit` | Entero | Sí | Número de transacción de débito |
| `transactionIdCredit` | Texto | Sí | Número de transacción de crédito |

#### Respuesta

| Elemento | Tipo de Dato | Descripción |
|----------|--------------|-------------|
| `responseCode` | Entero | Código de respuesta; diferente de cero indica error |
| `message` | Texto | Mensaje de error (si `responseCode` ≠ 0) |

---

## Anexo 1 – Definiciones de Objetos

### Objeto PaymentQR

Contiene la información de una transacción de pago QR.

| Nombre | Tipo de Dato | Descripción |
|--------|--------------|-------------|
| `qrId` | Texto | Identificador único del QR |
| `transactionId` | Texto | Número de transacción del banco |
| `paymentDate` | Fecha | Fecha del pago |
| `paymentTime` | Texto | Hora del pago |
| `currency` | Texto | Moneda: `BOB` o `USD` |
| `amount` | Decimal | Importe del pago recibido |
| `senderBankCode` | Texto | Código ASFI del banco origen |
| `senderName` | Texto | Nombre o Razón Social del remitente |
| `senderDocumentId` | Texto | No se usa (retorna cero) |
| `senderAccount` | Texto | Número de cuenta (ofuscado) |
| `description` | Texto | Descripción/glosa del QR |
| `branchCode` | Texto | Código de sucursal |

---

### Objeto AccountHeader

Información general de una cuenta.

| Nombre | Tipo de Dato | Descripción |
|--------|--------------|-------------|
| `accountCode` | Texto | Número de cuenta (encriptado) |
| `accountTypeCode` | Texto | Tipo: `CC` (Corriente) o `CA` (Ahorro) |
| `productName` | Texto | Nombre comercial del producto |
| `status` | Texto | Estado: `ACTIVA`, `INMOVILIZADA`, `CLAUSURADA`, `CERRADA` |
| `currency` | Texto | Moneda: `BOB` o `USD` |
| `balance` | Decimal | Saldo contable |
| `balanceReserved` | Decimal | Saldo reservado (pignorado) |
| `balanceRetained` | Decimal | Saldo retenido |
| `balanceAvailable` | Decimal | Saldo disponible |

---

### Objeto AccountDetail

Información de un movimiento de cuenta.

| Nombre | Tipo de Dato | Descripción |
|--------|--------------|-------------|
| `transactionId` | Entero | Número de transacción |
| `date` | Fecha | Fecha de transacción |
| `time` | Hora | Hora de transacción |
| `documentNumber` | Entero | Número de documento o cheque |
| `transactionType` | Texto | Tipo: `D` (Débito) o `C` (Crédito) |
| `amount` | Decimal | Importe (negativo para débitos, positivo para créditos) |
| `description` | Texto | Descripción de la transacción |
| `clienteNote` | Texto | Nota o glosa del cliente |

---

### Objeto AccountWithheld

Información de retenciones en cuenta.

| Nombre | Tipo de Dato | Descripción |
|--------|--------------|-------------|
| `transactionId` | Entero | Número de transacción de retención |
| `date` | Fecha | Fecha de la retención |
| `time` | Hora | Hora de la retención |
| `amount` | Decimal | Importe retenido |
| `description` | Texto | Descripción o motivo |
| `instruction` | Texto | Número de circular de la retención |
| `demanding` | Texto | Nombre del demandante |
| `judge` | Texto | Nombre del juez |
| `piet` | Texto | Proveído de inicio de ejecución tributaria |

---

### Objeto AMLData

Información de cumplimiento normativo (Anti-Money Laundering).

| Elemento | Tipo de Dato | Descripción |
|----------|--------------|-------------|
| `AMLSource` | Texto | Origen de los fondos |
| `AMLDestination` | Texto | Destino de los fondos |

---

### Objeto BatchPayment

Información de un pago dentro de una planilla.

| Nombre | Tipo de Dato | Descripción |
|--------|--------------|-------------|
| `batchDetailId` | Texto | Identificador único del pago |
| `amount` | Decimal | Importe del pago |
| `accountCode` | Texto | Número de cuenta beneficiaria |
| `accountTypeCode` | Texto | Tipo: `CCAD` (Corriente/Ahorro) o `CMOVILD` (Billetera Móvil) |
| `bankCode` | Texto | Código de la entidad financiera |
| `beneficiaryName` | Texto | Nombre del beneficiario |
| `beneficiaryDocId` | Texto | Documento de identidad (opcional) |
| `beneficiaryPhone` | Texto | Teléfono (opcional) |
| `beneficiaryEmail` | Texto | Email (opcional) |
| `note` | Texto | Nota o glosa del pago |
| `AMLData` | AMLData | Información AML |

---

## Historial de Cambios

| Versión | Fecha | Cambios |
|---------|-------|---------|
| v0.2.0 | 09/06/2021 | Descripción del algoritmo de encriptación. Se adiciona objeto payment en request |
| v0.3.0 | 15/06/2021 | Actualización de URLs. JSON con ejemplos de respuestas. Cambio de tipo de dato senderBankCode |
| v0.4.0 | 13/07/2021 | Corrección en nombre de método de autenticación |
| v0.5.0 | 23/07/2021 | URI de ambiente de certificación. Ejemplos JSON para PaidQR |
| v0.6.0 | 21/07/2022 | Diagrama de secuencia de pago con QR Simple |
| v1.0.0 | 07/11/2022 | Documentación y ejemplos de encriptación y desencriptación |
| v1.1.0 | 16/12/2024 | Nuevos métodos GET /v2/statusQR y /v2/paidQR |
| v1.2.0 | 13/02/2025 | Actualización de request en api generateQr |
| v1.3.0 | 28/04/2025 | Servicios de consultas de movimientos y carga de planillas de pagos |

---

## Notas Importantes

### Seguridad

- Todos los datos sensibles deben ser encriptados usando el algoritmo AES 256 bits
- Los tokens deben incluirse en la cabecera como `Authorization: Bearer {token}`
- Las claves de encriptación son proporcionadas solo en ambientes de certificación y producción

### Encriptación de Cuentas

Campos que deben ser encriptados:
- `accountCredit`
- `accountCode`
- Números de cuenta beneficiarios

### Formatos de Fecha

- Fechas: `yyyy-MM-dd`
- Fechas en parámetros de ruta (paidQR): `yyyyMMdd`
- Fechas con hora: `yyyy-MM-ddTHH:mm:ss`

### Límites y Restricciones

- Código de sucursal: máximo 5 caracteres
- Importe con decimales: máximo 2 decimales
- Monedas soportadas: BOB (bolivianos) y USD (dólares americanos)

---

**Documento generado:** Especificaciones Técnicas API Market Banco Económico S.A. v1.3.0  
**Formato:** Markdown  
**Última actualización:** 28/04/2025
