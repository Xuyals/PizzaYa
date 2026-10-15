-- =====================================================================
-- PizzaYa - HU6: Perfil del cliente (favoritas, último pedido y cupones)
-- Ejecutar en Supabase DESPUÉS de 01, 02 y 08 (necesita las tablas usuarios,
-- pedidos y cupones). Es idempotente: se puede correr varias veces.
-- Orden de los scripts: 01, 02, 04, 05, 08, 06, 07 y 09 (ver README).
-- =====================================================================

-- Pizzas favoritas: la misma configuración de la HU1 (pizza, tamaño, borde, extras y quitados)
create table if not exists favoritas (
    id           serial primary key,
    usuario_id   int         not null references usuarios(id) on delete cascade,
    pizza_id     int         not null references pizzas(id) on delete cascade,
    tamano_id    int         not null references tamanos(id),
    borde_id     int         not null references bordes(id),
    extras_ids   int[]       not null default '{}',
    quitados_ids int[]       not null default '{}',
    creado_en    timestamptz not null default now()
);

-- Una misma configuración no se guarda dos veces para el mismo cliente
create unique index if not exists favoritas_unica
    on favoritas (usuario_id, pizza_id, tamano_id, borde_id, extras_ids, quitados_ids);

-- El pedido recuerda a qué cliente registrado pertenece (nulo = pedido sin cuenta)
alter table pedidos add column if not exists usuario_id int references usuarios(id) on delete set null;
create index if not exists pedidos_usuario_idx on pedidos (usuario_id, creado_en desc);

-- Cupones con vencimiento y, opcionalmente, un dueño (nulo = cupón general para todos los clientes)
alter table cupones add column if not exists vence_en timestamptz;
alter table cupones add column if not exists usuario_id int references usuarios(id) on delete cascade;

alter table favoritas enable row level security;
