# Metodología Kanban – PizzaYa

Resumen del tablero, las políticas, los roles y las reuniones del proyecto. La fuente es `Kanban_PizzaYa.xlsx` (hojas Tablero, Políticas, Roles, Reuniones y Métricas). Si algo cambia en la Operations Review, se actualiza el Excel y este documento.

## Tablero

Las tarjetas avanzan de izquierda a derecha solo si la siguiente columna tiene cupo.

| Columna | Límite WIP | Propósito |
|---------|-----------|-----------|
| Backlog | Sin límite | Historias pendientes de análisis. Sin compromiso inmediato |
| Ready | 5 | Historias definidas, estimadas y aprobadas por el PO |
| Analysis | 2 | Detalle de requisitos y diseño de la solución (opcional) |
| Development | 3 | Programación y pruebas unitarias |
| Testing | 2 | Pruebas contra los criterios de aceptación |
| Review | 1 | El Product Owner acepta o devuelve |
| Deployment | 1 | Puesta en producción |
| Done | Sin límite | Terminado y verificado |

## Políticas explícitas

### Definition of Ready (pasar a Ready)
- Tiene rol, necesidad y finalidad.
- Mínimo 3 criterios de aceptación.
- Estimación (puntos y horas), prioridad y clase de servicio.
- Sin dependencias pendientes con historias que sigan en Backlog.
- La aprueba el PO en el Replenishment.

### Definition of Done (pasar a Done)
- Código en la rama principal (`main`).
- Todos los criterios de aceptación pasan en Testing.
- El PO lo aprobó en Review.
- Desplegado.
- Fecha de Done registrada en la hoja Seguimiento.

### Límites WIP y pull
- Si la siguiente columna está llena, la tarjeta no avanza.
- Columna llena: antes de empezar algo nuevo, el equipo ayuda a liberarla (swarming), por ejemplo ayudando a probar.
- Cada equipo toma (pull) la tarjeta más prioritaria de Ready cuando tiene capacidad. Un equipo no tiene más de 1 tarjeta en Development al tiempo.
- No se asignan equipos a tarjetas que siguen en Backlog.
- Las historias pequeñas con criterios claros pueden pasar directo de Ready a Development (sin Analysis).

### Clases de servicio
Orden de prioridad dentro de Ready: Expedite > Fecha fija > Estándar > Intangible. Dentro de la misma clase: Crítica > Alta > Media > Baja.

| Clase | Uso |
|-------|-----|
| Expedite | Solo bugs críticos o urgencias reales en producción. Puede saltarse el WIP (máximo 1 a la vez) |
| Fecha fija | Trabajo con fecha de entrega obligatoria (ej. entrega al profesor) |
| Estándar | Funcionalidades planeadas, en orden de prioridad |
| Intangible | Mejoras sin urgencia (ej. perfil, deuda técnica). Se toman con capacidad libre |

### Bloqueos
- Tarjeta bloqueada: marcar "Sí" en Seguimiento con el motivo. Se revisa primero en la Daily.
- Si lleva más de 2 días bloqueada, el Kanban Coach la escala.

### Review y devoluciones
- Solo el Product Owner aprueba o devuelve una tarjeta en Review (Delivery Review).
- Si falla en Testing o Review, vuelve a Development con el motivo anotado y se prioriza sobre trabajo nuevo.

### Estimación
La guía usa tallas S/M/L; este proyecto usa puntos y horas.

## Flujo de ramas y Pull Request

- `main` es la rama principal: todo lo que esté en `main` cumple "código en la rama principal" de la Definition of Done.
- Cada historia se trabaja en su rama `feature/HUn-nombre` (o `feature/equipoN-...`).
- Para integrar se abre un Pull Request contra `main`. Los PR los une el Kanban Coach.
- Si la rama del equipo está atrasada, se integra `main` con **merge** (nunca rebase ni force push sobre ramas de los equipos) en una rama `integracion/...` y se abre el PR desde ahí.
- Commits en español con prefijo (`feat:`, `fix:`, `docs:`, `chore:`, `merge:`) y mensaje descriptivo; no usar mensajes como "1.0".
- Cada PR describe: qué se hizo, cómo probarlo y una sección "Pendiente del equipo" con lo que no cumple los criterios de aceptación.

## Roles

| Rol | Responsable | Qué hace |
|-----|-------------|----------|
| Product Owner | Por definir | Prioriza el Backlog y asigna la clase de servicio; aprueba qué pasa a Ready; acepta o rechaza en Review |
| Kanban Coach / Flow Manager | Santiago | Facilita las reuniones; detecta y elimina bloqueos; vigila los límites WIP; calcula y muestra las métricas |
| Equipos 1 a 5 (desarrollo) | Por definir | Toman tarjetas de Ready (pull), desarrollan, hacen pruebas unitarias y ayudan a liberar columnas llenas |
| QA / Tester | Por definir | Prueba cada tarjeta contra sus criterios en Testing (puede ser rol rotativo) |
| DevOps / Despliegue | Por definir | Pasa las tarjetas aprobadas a producción |

## Reuniones

| Reunión | Duración | Frecuencia | Qué se hace | Facilita |
|---------|----------|-----------|-------------|----------|
| Daily Kanban | 15 min | Diaria | Recorrer el tablero de derecha a izquierda: bloqueos, columnas sobre el WIP, swarming | Kanban Coach |
| Replenishment | 30 min | Semanal | El PO y el equipo escogen del Backlog las tarjetas a Ready según la Definition of Ready | Kanban Coach |
| Delivery Review | 15 min | Bajo demanda (cuando hay una tarjeta en Review) | Validar la tarjeta contra sus criterios: se acepta o se devuelve con comentarios | Kanban Coach |
| Operations Review | 1 hora | Mensual | Revisar métricas, ajustar WIP y políticas (asisten PO, equipos y el profesor) | Kanban Coach |

Los días y horas están por definir en el Excel.

## Métricas

Se calculan en el Excel desde la hoja Seguimiento: tarjetas terminadas, lead time (Backlog a Done), cycle time (Ready a Done), throughput semanal, tarjetas bloqueadas, WIP total y flujo acumulado (CFD).

## Historias de usuario

| HU | Historia | Equipo | Puntos | Prioridad | Clase |
|----|----------|--------|--------|-----------|-------|
| 1 | Armar mi pizza (pizza, tamaño, borde, ingredientes) | 1 | 8 | Alta | Estándar |
| 2 | Cálculo automático del precio | 2 | 8 | Crítica | Expedite |
| 3 | Carrito y confirmación del pedido | 2 | 5 | Alta | Estándar |
| 4 | Ver y cambiar estado de pedidos | 3 | 8 | Alta | Estándar |
| 5 | Inventario y pedidos a proveedores | 4 | 8 | Media | Estándar |
| 6 | Perfil: favoritas, último pedido y cupones | 5 | 5 | Baja | Intangible |

Las historias sugeridas 7 (administrar el menú), 8 (registro e inicio de sesión) y 9 (descontar inventario al confirmar) están en Backlog pendientes de aprobación del PO. Los criterios de aceptación están en el Excel y no se copian aquí para no duplicarlos.
