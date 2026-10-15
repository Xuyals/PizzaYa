-- =====================================================================
-- PizzaYa - HU9 (sugerida): Descontar el inventario al confirmar un pedido
-- Ejecutar en Supabase DESPUÉS de 05 (necesita las columnas de inventario).
-- Es idempotente: se puede correr varias veces.
-- Orden de los scripts: 01, 02, 04, 05, 08, 06, 07 y 09 (ver README).
-- =====================================================================

-- Cuánto gasta de este ingrediente una pizza PERSONAL, en la misma unidad del inventario
-- (kg o L). Una mediana o familiar gasta esa cantidad por el factor de su tamaño.
-- Valor inicial: 0.1 por ingrediente; ajustarlo con un update, por ejemplo:
--   update ingredientes set consumo_por_pizza = 0.15 where nombre = 'Queso mozzarella';
alter table ingredientes
    add column if not exists consumo_por_pizza numeric(10,3) not null default 0.1
    check (consumo_por_pizza > 0);
