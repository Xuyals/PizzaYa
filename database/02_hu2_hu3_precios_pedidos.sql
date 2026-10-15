-- =====================================================================
-- PizzaYa - HU2 y HU3: Cupones de descuento y Pedidos (Equipo 2)
-- Ejecutar en Supabase: SQL Editor > New query > pegar y "Run".
-- Es idempotente: se puede correr varias veces sin duplicar datos.
-- =====================================================================

-- Tabla de cupones de descuento (HU2)
create table if not exists cupones (
    id              serial primary key,
    codigo          text          not null unique,
    monto_descuento numeric(10,2) not null check (monto_descuento > 0),
    descripcion     text          not null default '',
    activo          boolean       not null default true,
    creado_en       timestamptz   not null default now()
);

-- Tabla de pedidos (HU3)
create table if not exists pedidos (
    id              serial primary key,
    numero_pedido   text          not null unique,
    nombre_cliente  text          not null,
    telefono        text          not null,
    es_domicilio    boolean       not null default false,
    direccion       text,
    subtotal        numeric(10,2) not null,
    costo_domicilio numeric(10,2) not null default 0,
    descuento       numeric(10,2) not null default 0,
    total           numeric(10,2) not null,
    metodo_pago     text not null default 'Antes de la entrega',
    estado          text          not null default 'Recibido',
    creado_en       timestamptz   not null default now()
);

alter table pedidos add column if not exists metodo_pago text not null default 'Antes de la entrega';

-- Esta es la única tabla de pedidos: la usan el carrito (HU3) y el módulo de pedidos (HU4).
-- Si ya existía con el estado inicial anterior, se ajusta sin perder datos.
alter table pedidos alter column estado set default 'Recibido';
update pedidos set estado = 'Recibido' where estado = 'pendiente';

-- Items del pedido (HU3)
create table if not exists pedido_items (
    id              serial primary key,
    pedido_id       int           not null references pedidos(id) on delete cascade,
    pizza_id        int           not null references pizzas(id),
    tamano_id       int           not null references tamanos(id),
    borde_id        int           not null references bordes(id),
    cantidad        int           not null default 1 check (cantidad > 0),
    precio_unitario numeric(10,2) not null,
    subtotal        numeric(10,2) generated always as (cantidad * precio_unitario) stored,
    nombre_pizza    text          not null,
    nombre_tamano   text          not null,
    nombre_borde    text          not null
);

-- Extras elegidos por ítem de pedido (HU3)
create table if not exists pedido_item_extras (
    pedido_item_id  int not null references pedido_items(id) on delete cascade,
    ingrediente_id  int not null references ingredientes(id),
    primary key (pedido_item_id, ingrediente_id)
);

-- Ingredientes quitados por ítem de pedido (HU3)
create table if not exists pedido_item_quitados (
    pedido_item_id  int not null references pedido_items(id) on delete cascade,
    ingrediente_id  int not null references ingredientes(id),
    primary key (pedido_item_id, ingrediente_id)
);

-- RLS: solo la app (conexión directa) puede escribir; la API pública no puede leer pedidos.
alter table cupones          enable row level security;
alter table pedidos          enable row level security;
alter table pedido_items     enable row level security;
alter table pedido_item_extras   enable row level security;
alter table pedido_item_quitados enable row level security;

-- Los cupones son de lectura pública (el menú los lista)
drop policy if exists "cupones lectura publica" on cupones;
create policy "cupones lectura publica" on cupones for select using (activo = true);

-- Datos iniciales de cupones
insert into cupones (codigo, monto_descuento, descripcion) values
    ('PIZZA10',   5000, 'Descuento $5.000'),
    ('PROMO20',  10000, 'Descuento $10.000'),
    ('GRATIS',   15000, 'Descuento $15.000')
on conflict (codigo) do nothing;
