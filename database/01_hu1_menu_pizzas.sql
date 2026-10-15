-- =====================================================================
-- PizzaYa - HU1: Menú de pizzas y personalización (Equipo 1)
-- Ejecutar en Supabase: SQL Editor > New query > pegar y "Run".
-- Es idempotente: se puede correr varias veces sin duplicar datos.
--
-- Tablas compartidas con otros equipos:
--   * ingredientes -> el Equipo 4 (inventario) puede agregarle columnas
--     como cantidad o cantidad_minima con ALTER TABLE.
--   * tamanos.factor_precio y bordes.precio -> los usa el Equipo 2
--     para calcular el precio.
-- =====================================================================

create table if not exists tamanos (
    id            serial primary key,
    nombre        text          not null unique,
    factor_precio numeric(4,2)  not null default 1 check (factor_precio > 0),
    orden         int           not null default 0
);

create table if not exists bordes (
    id     serial primary key,
    nombre text          not null unique,
    precio numeric(10,2) not null default 0 check (precio >= 0),
    orden  int           not null default 0
);

create table if not exists ingredientes (
    id           serial primary key,
    nombre       text          not null unique,
    precio_extra numeric(10,2) not null default 0 check (precio_extra >= 0),
    disponible   boolean       not null default true
);

create table if not exists pizzas (
    id          serial primary key,
    nombre      text          not null unique,
    descripcion text,
    precio_base numeric(10,2) not null check (precio_base >= 0),
    imagen_url  text,
    disponible  boolean       not null default true,
    creado_en   timestamptz   not null default now()
);

create table if not exists pizza_ingredientes (
    pizza_id       int not null references pizzas(id) on delete cascade,
    ingrediente_id int not null references ingredientes(id),
    primary key (pizza_id, ingrediente_id)
);

-- Seguridad: la app se conecta directo a Postgres, así que RLS no la afecta.
-- Se activa para que la API pública de Supabase solo pueda LEER el menú.
alter table tamanos            enable row level security;
alter table bordes             enable row level security;
alter table ingredientes       enable row level security;
alter table pizzas             enable row level security;
alter table pizza_ingredientes enable row level security;

drop policy if exists "menu lectura publica" on tamanos;
drop policy if exists "menu lectura publica" on bordes;
drop policy if exists "menu lectura publica" on ingredientes;
drop policy if exists "menu lectura publica" on pizzas;
drop policy if exists "menu lectura publica" on pizza_ingredientes;
create policy "menu lectura publica" on tamanos            for select using (true);
create policy "menu lectura publica" on bordes             for select using (true);
create policy "menu lectura publica" on ingredientes       for select using (true);
create policy "menu lectura publica" on pizzas             for select using (true);
create policy "menu lectura publica" on pizza_ingredientes for select using (true);

-- ---------------------------------------------------------------------
-- Datos iniciales
-- ---------------------------------------------------------------------
insert into tamanos (nombre, factor_precio, orden) values
    ('Personal', 1.00, 1),
    ('Mediana',  1.60, 2),
    ('Familiar', 2.20, 3)
on conflict (nombre) do nothing;

insert into bordes (nombre, precio, orden) values
    ('Tradicional',            0, 1),
    ('Relleno de queso',    5000, 2),
    ('Ajo y finas hierbas', 3000, 3),
    ('Sin borde',              0, 4),
    ('Relleno de bocadillo', 4500, 5),
    ('Relleno de chocolate', 5000, 6)
on conflict (nombre) do nothing;

insert into ingredientes (nombre, precio_extra) values
    ('Queso mozzarella', 3000),
    ('Salsa de tomate',     0),
    ('Jamón',            3500),
    ('Piña',             2500),
    ('Pepperoni',        4000),
    ('Pollo',            4000),
    ('Champiñones',      3000),
    ('Maíz tierno',      2000),
    ('Jalapeños',        2500),
    ('Carne molida',     4000),
    ('Pimentón',         2000),
    ('Cebolla',          1500),
    ('Aceitunas',        3000),
    ('Tocineta',         4500),
    ('Queso extra',       3000),
    ('Carne',             4500),
    ('Chorizo',           4000),
    ('Guacamole',         3500),
    ('Chicharrón',        4500),
    ('Carne de cerdo',    4500),
    ('Masa',                 0),
    ('Pasta de tomate',      0),
    ('Queso base',            0)
on conflict (nombre) do nothing;

insert into pizzas (nombre, descripcion, precio_base) values
    ('Hawaiana',               'La clásica dulce y salada.',            28000),
    ('Pepperoni',              'Abundante pepperoni sobre mozzarella.', 30000),
    ('Mexicana',               'Picante, con carne y jalapeños.',       32000),
    ('Vegetariana',            'Llena de vegetales frescos.',           27000),
    ('Pollo con champiñones',  'Pollo desmechado y champiñones.',       31000)
on conflict (nombre) do nothing;

insert into pizza_ingredientes (pizza_id, ingrediente_id)
select p.id, i.id
from (values
    ('Hawaiana', 'Salsa de tomate'), ('Hawaiana', 'Queso mozzarella'), ('Hawaiana', 'Jamón'), ('Hawaiana', 'Piña'),
    ('Pepperoni', 'Salsa de tomate'), ('Pepperoni', 'Queso mozzarella'), ('Pepperoni', 'Pepperoni'),
    ('Mexicana', 'Salsa de tomate'), ('Mexicana', 'Queso mozzarella'), ('Mexicana', 'Carne molida'),
    ('Mexicana', 'Jalapeños'), ('Mexicana', 'Maíz tierno'), ('Mexicana', 'Cebolla'),
    ('Vegetariana', 'Salsa de tomate'), ('Vegetariana', 'Queso mozzarella'), ('Vegetariana', 'Champiñones'),
    ('Vegetariana', 'Pimentón'), ('Vegetariana', 'Cebolla'), ('Vegetariana', 'Aceitunas'), ('Vegetariana', 'Maíz tierno'),
    ('Pollo con champiñones', 'Salsa de tomate'), ('Pollo con champiñones', 'Queso mozzarella'),
    ('Pollo con champiñones', 'Pollo'), ('Pollo con champiñones', 'Champiñones')
) as v(pizza, ingrediente)
join pizzas p       on p.nombre = v.pizza
join ingredientes i on i.nombre = v.ingrediente
on conflict do nothing;

insert into pizzas (nombre, descripcion, precio_base)
values ('Arma tu pizza', 'Crea una pizza a tu gusto con ingredientes seleccionados.', 22000)
on conflict (nombre) do nothing;

insert into pizza_ingredientes (pizza_id, ingrediente_id)
select p.id, i.id
from pizzas p
join ingredientes i on i.nombre in ('Masa', 'Pasta de tomate', 'Queso base')
where p.nombre = 'Arma tu pizza'
on conflict do nothing;
