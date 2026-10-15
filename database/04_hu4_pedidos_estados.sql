-- =====================================================================
-- PizzaYa - HU4: Panel de cocina y estados de pedidos (Equipo 3)
-- Ejecutar en Supabase DESPUÉS de 02_hu2_hu3_precios_pedidos.sql.
-- Es idempotente: se puede correr varias veces.
--
-- La tabla pedidos es la de HU2/HU3. Aquí solo se asegura que el estado sea
-- uno de los cuatro válidos y se agregan índices para el panel de cocina.
-- =====================================================================

update pedidos set estado = 'Recibido' where estado = 'pendiente';

alter table pedidos drop constraint if exists pedidos_estado_valido;
alter table pedidos add constraint pedidos_estado_valido
    check (estado in ('Recibido', 'En preparación', 'En entrega', 'Entregado'));

create index if not exists pedidos_creado_en_idx on pedidos (creado_en, id);
create index if not exists pedidos_estado_idx on pedidos (estado);
