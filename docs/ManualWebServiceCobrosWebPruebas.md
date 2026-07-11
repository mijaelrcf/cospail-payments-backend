# Manual Web Service COBROS-WEB (Ambiente de Pruebas)

## URL del ambiente de PRUEBAS

https://ws.cospail.com.bo/wstest/wsco.asmx

---

# Método: `ObtenerDeudaSocioDide`

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `liCFijo` | Número entero positivo denominado **Código Fijo**. |
| `lsDide` | Documento de identidad del asociado. También puede ser el número de NIT. |

**Ambos campos son obligatorios.**

Si el asociado tiene deuda, el servicio retorna:

```xml
<DataSet xmlns="http://sermix.net/">
  <xs:schema xmlns=""
             xmlns:xs="http://www.w3.org/2001/XMLSchema"
             xmlns:msdata="urn:schemas-microsoft-com:xml-msdata"
             id="NewDataSet">
    <xs:element name="NewDataSet"
                msdata:IsDataSet="true"
                msdata:UseCurrentLocale="true">
      <xs:complexType>
        <xs:choice minOccurs="0" maxOccurs="unbounded">
          <xs:element name="Table">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="NAviso" type="xs:int" minOccurs="0"/>
                <xs:element name="NCredito" type="xs:int" minOccurs="0"/>
                <xs:element name="Tipo" type="xs:int" minOccurs="0"/>
                <xs:element name="Anio" type="xs:int" minOccurs="0"/>
                <xs:element name="Mes" type="xs:int" minOccurs="0"/>
                <xs:element name="Nombre" type="xs:string" minOccurs="0"/>
                <xs:element name="Periodo" type="xs:string" minOccurs="0"/>
                <xs:element name="Deuda" type="xs:double" minOccurs="0"/>
              </xs:sequence>
            </xs:complexType>
          </xs:element>
        </xs:choice>
      </xs:complexType>
    </xs:element>
  </xs:schema>

  <diffgr:diffgram
      xmlns:msdata="urn:schemas-microsoft-com:xml-msdata"
      xmlns:diffgr="urn:schemas-microsoft-com:xml-diffgram-v1">

    <NewDataSet xmlns="">
      <Table diffgr:id="Table1" msdata:rowOrder="0">
        <NAviso>853827</NAviso>
        <NCredito>855475</NCredito>
        <Tipo>1</Tipo>
        <Anio>2023</Anio>
        <Mes>4</Mes>
        <Nombre>Rebeca Dina Lia Machaca</Nombre>
        <Periodo>Abr/2023</Periodo>
        <Deuda>93.73</Deuda>
      </Table>
    </NewDataSet>

  </diffgr:diffgram>
</DataSet>
```

## Descripción de los campos

| Campo | Descripción |
|--------|-------------|
| `NAviso` | Número de aviso entregado al asociado. |
| `NCredito` | Número asignado al crédito del aviso. |
| `Tipo` | Valor **1** para el cobro de avisos. |
| `Anio` | Año del aviso. |
| `Mes` | Mes del aviso. |
| `Nombre` | Nombre del asociado. |
| `Periodo` | Período del aviso. |
| `Deuda` | Importe total del aviso. |

---

# Caso: Código fijo inexistente

Si el código fijo no existe en la base de datos retorna:

```xml
<DataSet xmlns="http://sermix.net/">
  <xs:schema ...>
    ...
  </xs:schema>

  <diffgr:diffgram
      xmlns:msdata="urn:schemas-microsoft-com:xml-msdata"
      xmlns:diffgr="urn:schemas-microsoft-com:xml-diffgram-v1">

    <NewDataSet xmlns="">
      <Table diffgr:id="Table1"
             msdata:rowOrder="0"
             diffgr:hasChanges="inserted">
        <NAviso>0</NAviso>
        <NCredito>0</NCredito>
        <Tipo>0</Tipo>
        <Anio>0</Anio>
        <Mes>0</Mes>
        <Nombre>NO EXISTE ASOCIDO YYYY</Nombre>
        <Periodo/>
        <Deuda>-1</Deuda>
      </Table>
    </NewDataSet>

  </diffgr:diffgram>
</DataSet>
```

## Interpretación

- En **Nombre** aparece:

  ```
  NO EXISTE ASOCIADO YYYY
  ```

- **Deuda** retorna:

  ```
  -1
  ```

---

# Caso: Código fijo existe pero el CI/NIT no coincide

Retorna:

```xml
<DataSet xmlns="http://sermix.net/">
  <xs:schema ...>
    ...
  </xs:schema>

  <diffgr:diffgram
      xmlns:msdata="urn:schemas-microsoft-com:xml-msdata"
      xmlns:diffgr="urn:schemas-microsoft-com:xml-diffgram-v1">

    <NewDataSet xmlns="">
      <Table diffgr:id="Table1"
             msdata:rowOrder="0"
             diffgr:hasChanges="inserted">
        <NAviso>0</NAviso>
        <NCredito>0</NCredito>
        <Tipo>0</Tipo>
        <Anio>0</Anio>
        <Mes>0</Mes>
        <Nombre>Gutierrez Vega Jaime</Nombre>
        <Periodo>NO COINCIDE CI/NIT:NNN</Periodo>
        <Deuda>0</Deuda>
      </Table>
    </NewDataSet>

  </diffgr:diffgram>
</DataSet>
```

## Interpretación

- **Nombre** contiene el nombre del asociado.
- **Periodo** contiene:

  ```
  NO COINCIDE CI/NIT:NNN
  ```

- **Deuda** retorna:

  ```
  0
  ```

---

# Caso: Código fijo existe, CI/NIT coincide y no tiene deuda

Retorna:

```xml
<DataSet xmlns="http://sermix.net/">
  <xs:schema ...>
    ...
  </xs:schema>

  <diffgr:diffgram
      xmlns:msdata="urn:schemas-microsoft-com:xml-msdata"
      xmlns:diffgr="urn:schemas-microsoft-com:xml-diffgram-v1">

    <NewDataSet xmlns="">
      <Table diffgr:id="Table1"
             msdata:rowOrder="0"
             diffgr:hasChanges="inserted">
        <NAviso>0</NAviso>
        <NCredito>0</NCredito>
        <Tipo>0</Tipo>
        <Anio>0</Anio>
        <Mes>0</Mes>
        <Nombre>Gutierrez Vega Jaime</Nombre>
        <Periodo>SIN DEUDA</Periodo>
        <Deuda>0</Deuda>
      </Table>
    </NewDataSet>

  </diffgr:diffgram>
</DataSet>
```

## Interpretación

- **Nombre** contiene el nombre correspondiente al código fijo.
- **Periodo** contiene:

  ```
  SIN DEUDA
  ```

- **Deuda** retorna:

  ```
  0
  ```

---

# Método: `grabarCobranzaWEB`

Permite registrar el cobro de una deuda.

## Retorno

- Retorna **1** cuando el proceso finaliza correctamente.
- Si el valor es distinto de **1**, corresponde al mensaje de error.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `NCredito` | Número del crédito del aviso. Debe enviarse el valor obtenido mediante `ObtenerDeudaSocioDide`. |
| `Tipo` | Valor obtenido mediante `ObtenerDeudaSocioDide`. |
| `Deuda` | Importe cobrado obtenido mediante `ObtenerDeudaSocioDide`. |
| `ldFpag` | Fecha en que se realiza el cobro del aviso. |
| `lsHpag` | Hora en que se realizó el cobro. |
| `lsLogin` | Usuario proporcionado por la cooperativa. |
| `lsPassword` | Clave proporcionada por la cooperativa. |

---

# Método: `obtenerUnaFacturaPDFB64`

Se utiliza para la impresión de la factura.

## Retorno

Devuelve un documento **PDF** convertido a una cadena en **Base64**.

---

# Método: `AnulaCobro`

Permite anular un cobro.

## Retorno

Retorna **1** cuando la operación se realiza correctamente.

---

# Reporte de cobros

Permite obtener el reporte de cobros para una fecha determinada.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `liCfijo` | Código fijo del asociado. Si se desea consultar todos, enviar **0**. |
| `fecha desde` | Fecha inicial del reporte. |
| `fecha hasta` | Fecha final del reporte. |
| `lsLogin` | Usuario proporcionado por la cooperativa. |
| `lsPassword` | Clave proporcionada por la cooperativa. |

## Retorno

No especificado en el documento.