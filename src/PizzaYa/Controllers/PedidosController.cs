using Microsoft.AspNetCore.Mvc;
using PizzaYa.Models;
using PizzaYa.Repositories;

namespace PizzaYa.Controllers
{
    public class PedidosController : Controller
    {
        private readonly IPedidoRepository _pedidoRepository;

        public PedidosController(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository;
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Crear(Pedido pedido)
        {
            if (ModelState.IsValid)
            {
                var pedidoCreado = await _pedidoRepository.CrearPedidoAsync(pedido);
                return RedirectToAction("Seguimiento", new { id = pedidoCreado.Id });
            }
            return View(pedido);
        }

        [HttpGet]
        public async Task<IActionResult> Seguimiento(int id)
        {
            var pedido = await _pedidoRepository.ObtenerPorIdAsync(id);
            if (pedido == null)
            {
                return NotFound();
            }
            return View(pedido);
        }
    }
}