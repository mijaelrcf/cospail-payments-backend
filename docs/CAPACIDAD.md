# Capacidad — Pagos Cospail

> Fuente: tabla mensual del negocio (ene–ago) + `docs/FechaHoraPagos.txt` (11.566 filas, 12/01/2026 al 30/06/2026, cuadra exacto con ene–jun).
> Formato del txt: `d/m/yyyy HH:mm` (hora local). Cada fila = 1 pago.

## 1. Pagos por mes

| Mes | Pagos |
|---|---|
| Enero | 1.614 |
| Febrero | 1.937 |
| Marzo | 2.118 |
| Abril | 1.665 |
| Mayo | 2.048 |
| Junio | 2.184 |
| Julio | 2.226 |
| Agosto | 2.305 |

* Total 8 meses: **16.097**. Promedio mensual: **~2.012** (desv. ~239).
* Mínimo: 1.614 (ene) / Máximo: 2.305 (ago). Crecimiento ene→ago: **+43%**.
* Promedio diario mensual: **~67/día** (pico mensual: ago ~74/día).

## 2. Pagos por día (del txt, 160 días)

* Promedio: **72/día**. p95: **145/día** (el 95% de los días tuvo ≤145 pagos; solo ~8 días lo superaron). Máximo: **195** (13/01/2026). Mínimo: **15**.
  > p95 = percentil 95: ordenas los 160 días de menor a mayor y tomas el valor que deja al 95% por debajo. Se usa para capacidad porque el promedio se queda corto la mitad de los días y el máximo sobredimensiona por un caso raro; se diseña para p95 con margen hasta el máximo.
* Top 5: 13/01 (195), 12/01 (186), 10/02 (184), 14/01 y 10/03 (166).
* Patrón semanal (lun=0): lun 2.212, mar 2.074, mié 1.999, jue 1.995, vie 1.404, sáb 1.075, dom 807. El finde cae ~50%.

## 3. Horario de más uso (del txt)

Pico **09–11h** (~10% del total cada hora). El ~60% del tráfico está entre 08–16h. Valle 00–05h (<1% c/hora, casi todo batch nocturno).

| H | % | H | % | H | % | H | % |
|---|---|---|---|---|---|---|---|
| 00 | 0,4 | 06 | 1,2 | 12 | 7,3 | 18 | 4,0 |
| 01 | 0,4 | 07 | 2,9 | 13 | 6,5 | 19 | 3,9 |
| 02 | 0,6 | 08 | 5,0 | 14 | 7,2 | 20 | 3,8 |
| 03 | 0,3 | 09 | 9,4 | 15 | 6,9 | 21 | 3,6 |
| 04 | 0,4 | 10 | 9,6 | 16 | 6,2 | 22 | 2,6 |
| 05 | 0,7 | 11 | 10,0 | 17 | 5,8 | 23 | 1,2 |

## 4. Picos observados

* **35 pagos en 1 hora** (06/05 09h; le siguen 34 y 32 en la misma franja 09h/11h de mayo).
* **13 pagos en 1 minuto** (19/01 15:31; siguiente 9 en 16/03 17:46).
* **21 pagos en 5 minutos** (30/04 04:59–05:02, batch de madrugada, no tráfico humano).
* Solo 57 minutos en 6 meses con ≥5 pagos; 8 con ≥8.

## 5. Concurrencia y capacidad del server

* Concurrencia real: **1–3 usuarios pagando a la vez**, ráfagas de **~10–13/min**.
* Cada pago genera 5–10 HTTP (`active-qr` + `initiate` + `generate-qr` + polling `payments/{id}` cada 2–3s + `notify` del banco). Pico HTTP estimado: 35 pagos/h × 10 ÷ 3600 ≈ **0,1 req/s medio, ráfagas de 2–5 req/s**.
* Umbrales de alerta sugeridos: hora >50 pagos, minuto >15, día >200.

### Conclusión

**Cualquier VPS lo soporta para empezar y no habrá problemas de concurrencia.** Un VPS 2vCPU/4GB (Vultr SP $20 o Hostinger BR $9–15) irá con >70% idle. El cuello real no es CPU sino los downstream: SOAP Cospail (`grabarCobrosWEB` secuencial por deuda) y API BanEco. Configurar pool Postgres 20–50, timeouts cortos, rate-limit en `notifyPaymentQR` (idempotente por `qr_id` UNIQUE) y conciliar con `paid-qr/{fecha}` los días 10–13 y la franja 09–11h.

### ¿Cuándo habría problemas?

Margen aproximado de **50–100x el pico actual**:

* Kestrel + PG local aguanta 500–2.000 req/s simples vs ráfagas actuales de 2–5 req/s.
* Saturación estimada: **200–500 usuarios pagando al mismo tiempo sostenido**, o **~50–100 pagos/minuto** (vs 13 actuales), o **~1.000 browsers en polling** cada 2s (~500 req/s). Ahí habría colas en pool/threads y timeouts al SOAP.
* En la práctica limita antes el banco (rate-limit) que el VPS. Con 10x el pico actual seguirías bien; habría que escalar recién con 10x sostenido (~3.300→33.000/mes).
