using Microsoft.AspNetCore.Mvc;
using PizzaYa.Models;
using System.Collections.Generic;

namespace PizzaYa.Controllers
{
    public class PedidosController : Controller
    {
        // Lista simulada temporal para almacenar pedidos en memoria
        private static List<Pedido> _pedidosSimulados = new();

        // GET: /Pedidos/Crear
        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        // POST: /Pedidos/Crear
        [HttpPost]
        public IActionResult Crear(Pedido pedido)
        {
            if (ModelState.IsValid)
            {
                pedido.Id = _pedidosSimulados.Count + 1;
                pedido.FechaCreacion = System.DateTime.Now;
                pedido.Estado = "Pendiente";
                _pedidosSimulados.Add(pedido);

                // Redirige a la vista de seguimiento con el ID del pedido
                return RedirectToAction("Seguimiento", new { id = pedido.Id });
            }
            return View(pedido);
        }

        // GET: /Pedidos/Seguimiento/5
        public IActionResult Seguimiento(int id)
        {
            var pedido = _pedidosSimulados.Find(p => p.Id == id);
            if (pedido == null)
            {
                // Objeto por defecto si no se encuentra en la lista temporal
                pedido = new Pedido 
                { 
                    Id = id, 
                    ClienteNombre = "Cliente de Prueba", 
                    DireccionEntrega = "Calle Principal #123",
                    Estado = "En Preparación" 
                };
            }
            return View(pedido);
        }
    }
}