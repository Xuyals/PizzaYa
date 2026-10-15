-- =====================================================================
-- PizzaYa - HU8 (sugerida): Registro e inicio de sesión
-- Ejecutar en Supabase: SQL Editor > New query > pegar y "Run".
-- Es idempotente: se puede correr varias veces sin duplicar datos.
-- Orden de los scripts: 01, 02, 04, 05, 08, 06, 07 y 09 (ver README).
--
-- La contraseña nunca se guarda: password_hash es el hash de ASP.NET Core Identity.
-- Quien se registra queda como 'Cliente'. Para crear el primer administrador
-- o un cocinero, ver "Cómo crear el primer usuario administrador" en el README.
-- =====================================================================

create table if not exists usuarios (
    id            serial primary key,
    nombre        text        not null,
    correo        text        not null,
    password_hash text        not null,
    rol           text        not null default 'Cliente'
                  check (rol in ('Cliente', 'Administrador', 'Cocinero')),
    creado_en     timestamptz not null default now()
);

-- Un correo no se puede repetir, sin importar mayúsculas
create unique index if not exists usuarios_correo_unico on usuarios (lower(correo));

-- Solo la app (conexión directa) lee y escribe usuarios; la API pública no ve nada.
alter table usuarios enable row level security;
