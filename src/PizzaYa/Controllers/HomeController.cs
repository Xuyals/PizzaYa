using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PizzaYa.Models;

namespace PizzaYa.Controllers;

public class HomeController : Controller
{
    // La página de inicio es el menú (MenuController); aquí solo queda la página de error.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
