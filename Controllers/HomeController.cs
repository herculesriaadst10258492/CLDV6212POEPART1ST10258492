// File: Controllers/HomeController.cs
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ABCRetail;
using ABCRetail.Models;

namespace ABCRetail.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public HomeController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var vm = new HomeViewModel
            {
                CustomersCount = await _context.Customers.CountAsync(),
                ProductsCount = await _context.Products.CountAsync(),
                OrdersCount = await _context.Orders.CountAsync(),
                FeaturedProducts = await _context.Products.Take(4).ToListAsync()
            };
            return View(vm);
        }
    }
}
