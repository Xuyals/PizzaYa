-- =====================================================================
-- PizzaYa - HU7 (sugerida): Administrar el menú y el valor del domicilio
-- Ejecutar en Supabase DESPUÉS de 01. Es idempotente: se puede correr varias veces.
-- Orden de los scripts: 01, 02, 04, 05, 08, 06, 07 y 09 (ver README).
--
-- Las pizzas, bordes, extras (ingredientes) y tamaños ya están en 01: la administración
-- solo los lee y escribe. Pizzas ocultas = pizzas.disponible = false.
-- =====================================================================

-- Valores que cambia el administrador (por ahora, el valor del domicilio)
create table if not exists configuracion (
    clave          text        primary key,
    valor          text        not null,
    actualizado_en timestamptz not null default now()
);

insert into configuracion (clave, valor) values ('costo_domicilio', '5000')
on conflict (clave) do nothing;

-- Solo la app (conexión directa) lee y escribe la configuración
alter table configuracion enable row level security;
