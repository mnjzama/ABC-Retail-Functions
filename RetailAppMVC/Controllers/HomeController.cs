using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RetailAppMVC.Models;

namespace RetailAppMVC.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Optional: add a simple Dashboard view
        public IActionResult Dashboard()
        {
            return View();
        }

        // Optional: Error handling
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
