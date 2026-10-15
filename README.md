# PizzaYa
Tienda virtual de pizzas con pedidos a domicilio, hecha con ASP.NET Core y Supabase, y organizada con la metodología Kanban

## Estado del proyecto
Se trabaja con Kanban; el tablero, las políticas y los roles están en [docs/kanban.md](docs/kanban.md).

| HU | Historia | Equipo | Rama | Estado |
|----|----------|--------|------|--------|
| 1 | Menú de pizzas y personalización | 1 | `feature/HU1-pizzas` | En `main`, completa |
| 2 | Cálculo de precio | 2 | `feature/equipo2-hu2-hu3` | En PR de integración, pendiente de unir |
| 3 | Carrito y confirmación del pedido | 2 | `feature/equipo2-hu2-hu3` | En PR de integración, pendiente de unir |
| 4 | Pedidos y estados | 3 | `feature/HU4-pedidos_estados` | En `main`, incompleta (ver issue "HU4 incompleta") |
| 5 | Inventario y proveedores | 4 | `feature/HU5-inventario_proveedores` | En PR de integración, pendiente de unir |
| 6 | Perfil, favoritas y cupones | 5 | `feature/HU6-perfil` | Sin código todavía |

## Tecnologías
- ASP.NET Core MVC (.NET 8) con C#
- Supabase (Postgres) usando Npgsql + Dapper
- Bootstrap 5
- xUnit para las pruebas

## Estructura
```
database/                 Scripts SQL para correr en Supabase (uno por HU)
src/PizzaYa/
  Controllers/            Controladores MVC
  Models/                 Entidades y ViewModels
  Repositories/           Acceso a datos (Supabase y en memoria)
  Services/               Reglas de negocio
  Views/                  Vistas Razor
tests/PizzaYa.Tests/      Pruebas de los criterios de aceptación
```

## Cómo correrlo
1. Crear las tablas: en Supabase abrir **SQL Editor**, pegar `database/01_hu1_menu_pizzas.sql` y ejecutarlo.
2. Copiar la cadena de conexión: **Project Settings → Database → Connection string → .NET**
   (usar la del *Session pooler*) y guardarla como secreto local, **nunca en el repositorio**:
   ```bash
   cd src/PizzaYa
   dotnet user-secrets set "ConnectionStrings:Supabase" "Host=...;Port=5432;Database=postgres;Username=postgres.xxxx;Password=...;SSL Mode=Require"
   ```
3. Correr la app:
   ```bash
   dotnet run --project src/PizzaYa
   ```
   Si no hay cadena de conexión, la app arranca con un menú de ejemplo en memoria (sirve para maquetar sin base de datos).
4. Pruebas: `dotnet test`

## HU1 – Menú de pizzas y personalización (Equipo 1)
| # | Criterio | Dónde |
|---|----------|-------|
| 1 | Menú con nombre, ingredientes base, precio y botón "Personalizar" | `Views/Menu/Index.cshtml` |
| 2 | Escoger personal, mediana o familiar; no se continúa sin tamaño | `Personalizar.cshtml` + `PersonalizacionService` |
| 3 | El borde escogido queda registrado en la pizza | `PizzaPersonalizada.BordeId` |
| 4 | Agregar extras o quitar ingredientes base; máximo 6 extras | `PersonalizacionService` (servidor) + JS (cliente) |
| 5 | Sin pizzas disponibles se muestra "Pronto habrá pizzas" | `Views/Menu/Index.cshtml` |

### Para los demás equipos
- La pizza armada se guarda en la sesión con la clave `PizzaPersonalizada.ClaveSesion`
  como JSON de `PizzaPersonalizada` (`PizzaId`, `TamanoId`, `BordeId`, `ExtrasIds`, `QuitadosIds`).
  El **Equipo 2** la lee para calcular el precio (HU2) y agregarla al carrito (HU3) desde `Views/Menu/Resumen.cshtml`.
- Precios disponibles para la HU2: `pizzas.precio_base`, `tamanos.factor_precio`, `bordes.precio`, `ingredientes.precio_extra`.
- La tabla `ingredientes` es compartida con el **Equipo 4** (inventario): agregar columnas con `ALTER TABLE`, sin recrearla.
