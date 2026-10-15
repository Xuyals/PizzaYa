-- =====================================================================
-- PizzaYa - HU5: Inventario y pedidos a proveedores (Equipo 4)
-- Ejecutar en Supabase: SQL Editor > New query > pegar y "Run".
-- Requiere haber ejecutado antes 01_hu1_menu_pizzas.sql.
-- Es idempotente: se puede correr varias veces sin duplicar datos
-- ni reiniciar las cantidades.
--
-- La tabla ingredientes es compartida con el Equipo 1: aquí solo se le
-- AGREGAN columnas con ALTER TABLE, no se recrea.
-- =====================================================================

alter table ingredientes
    add column if not exists unidad          text          not null default 'kg',
    add column if not exists cantidad        numeric(10,2) not null default 0 check (cantidad >= 0),
    add column if not exists cantidad_minima numeric(10,2) not null default 0 check (cantidad_minima >= 0);

create table if not exists pedidos_proveedor (
    id          serial primary key,
    proveedor   text        not null check (length(trim(proveedor)) > 0),
    estado      text        not null default 'Pendiente' check (estado in ('Pendiente', 'Recibido')),
    creado_en   timestamptz not null default now(),
    recibido_en timestamptz
);

create table if not exists pedido_proveedor_items (
    pedido_id      int           not null references pedidos_proveedor(id) on delete cascade,
    ingrediente_id int           not null references ingredientes(id),
    cantidad       numeric(10,2) not null check (cantidad > 0),
    primary key (pedido_id, ingrediente_id)
);

-- Seguridad: la app se conecta directo a Postgres, así que RLS no la afecta.
-- Sin políticas, la API pública de Supabase NO puede leer ni escribir los pedidos.
alter table pedidos_proveedor      enable row level security;
alter table pedido_proveedor_items enable row level security;

-- ---------------------------------------------------------------------
-- Datos iniciales de inventario (solo si el ingrediente aún está en 0/0,
-- así volver a correr el script no pisa las cantidades reales).
-- Varios quedan bajo el mínimo para poder probar "Por agotarse".
-- ---------------------------------------------------------------------
update ingredientes i
set unidad = v.unidad, cantidad = v.cantidad, cantidad_minima = v.minimo
from (values
    ('Queso mozzarella', 'kg',  4.0, 10.0),
    ('Salsa de tomate',  'L',  12.0,  5.0),
    ('Jamón',            'kg', 10.0,  4.0),
    ('Piña',             'kg',  7.0,  3.0),
    ('Pepperoni',        'kg',  2.0,  5.0),
    ('Pollo',            'kg',  3.0,  8.0),
    ('Champiñones',      'kg',  1.5,  3.0),
    ('Maíz tierno',      'kg',  6.0,  3.0),
    ('Jalapeños',        'kg',  4.0,  2.0),
    ('Carne molida',     'kg', 12.0,  6.0),
    ('Pimentón',         'kg',  5.0,  2.0),
    ('Cebolla',          'kg',  9.0,  4.0),
    ('Aceitunas',        'kg',  3.0,  2.0),
    ('Tocineta',         'kg',  4.0,  2.0)
) as v(nombre, unidad, cantidad, minimo)
where i.nombre = v.nombre
  and i.cantidad = 0
  and i.cantidad_minima = 0;
