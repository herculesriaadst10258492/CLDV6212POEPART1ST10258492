using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ABCRetail;
using ABCRetail.Models;

namespace ABCRetail.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        public OrderController(ApplicationDbContext context) => _context = context;

        // ----- Helpers: EF key + conversion -----
        private (string Name, Type Type) KeyMeta()
        {
            var et = _context.Model.FindEntityType(typeof(Order));
            var pk = et?.FindPrimaryKey()?.Properties.FirstOrDefault();
            return (pk?.Name ?? "Id", pk?.ClrType ?? typeof(int));
        }
        private object ConvertId(string id)
        {
            var (_, t) = KeyMeta();
            if (t == typeof(int)) return int.Parse(id);
            if (t == typeof(Guid)) return Guid.Parse(id);
            return id;
        }
        private string KeyString(Order o)
        {
            var (name, _) = KeyMeta();
            var prop = o.GetType().GetProperty(name);
            object? val = prop != null ? prop.GetValue(o) : _context.Entry(o).Property(name).CurrentValue;
            return val?.ToString() ?? string.Empty;
        }

        // ----- Dropdown population (covers any naming the view might use) -----
        private void PopulateDropdowns(Order? current = null)
        {
            // Try to infer property names commonly used in views
            string? curCustomerId = current?.GetType().GetProperties().FirstOrDefault(p => p.Name.EndsWith("CustomerId", StringComparison.OrdinalIgnoreCase))?.GetValue(current)?.ToString();
            string? curProductId = current?.GetType().GetProperties().FirstOrDefault(p => p.Name.EndsWith("ProductId", StringComparison.OrdinalIgnoreCase))?.GetValue(current)?.ToString();

            // Build customer options
            var custs = _context.Customers.AsNoTracking().ToList()
                .Select(c =>
                {
                    var et = _context.Model.FindEntityType(typeof(Customer));
                    var pk = et?.FindPrimaryKey()?.Properties.FirstOrDefault();
                    var keyName = pk?.Name ?? "Id";
                    var keyVal = c.GetType().GetProperty(keyName)?.GetValue(c)
                               ?? _context.Entry(c).Property(keyName).CurrentValue;
                    return new SelectListItem
                    {
                        Value = keyVal?.ToString(),
                        Text = c.GetType().GetProperty("Name")?.GetValue(c)?.ToString()
                             ?? c.GetType().GetProperty("FullName")?.GetValue(c)?.ToString()
                             ?? c.GetType().GetProperty("Email")?.GetValue(c)?.ToString()
                             ?? $"Customer {keyVal}",
                        Selected = curCustomerId != null && keyVal?.ToString() == curCustomerId
                    };
                })
                .ToList();

            // Build product options
            var prods = _context.Products.AsNoTracking().ToList()
                .Select(p =>
                {
                    var et = _context.Model.FindEntityType(typeof(Product));
                    var pk = et?.FindPrimaryKey()?.Properties.FirstOrDefault();
                    var keyName = pk?.Name ?? "Id";
                    var keyVal = p.GetType().GetProperty(keyName)?.GetValue(p)
                               ?? _context.Entry(p).Property(keyName).CurrentValue;

                    var name = p.GetType().GetProperty("Name")?.GetValue(p)?.ToString() ?? $"Product {keyVal}";
                    var price = p.GetType().GetProperty("Price")?.GetValue(p)?.ToString();
                    var label = string.IsNullOrWhiteSpace(price) ? name : $"{name} (${price})";

                    return new SelectListItem
                    {
                        Value = keyVal?.ToString(),
                        Text = label,
                        Selected = curProductId != null && keyVal?.ToString() == curProductId
                    };
                })
                .ToList();

            // Set many common keys so whatever the view expects, it finds it
            ViewBag.Customers = custs;
            ViewBag.CustomerList = custs;
            ViewBag.CustomerOptions = custs;
            ViewData["Customers"] = custs;

            ViewBag.Products = prods;
            ViewBag.ProductList = prods;
            ViewBag.ProductOptions = prods;
            ViewData["Products"] = prods;
        }

        // ----- Index -----
        public async Task<IActionResult> Index()
            => View(await _context.Orders.AsNoTracking().ToListAsync());

        // ----- Details -----
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var key = ConvertId(id);
            var order = await _context.Orders.FindAsync(key);
            if (order == null) return NotFound();
            ViewBag.KeyId = KeyString(order);
            return View(order);
        }

        // ----- Create -----
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View(new Order());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(order);
                return View(order);
            }
            _context.Add(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ----- Edit -----
        public async Task<IActionResult> Edit(string id)
        {
            var key = ConvertId(id);
            var order = await _context.Orders.FindAsync(key);
            if (order == null) return NotFound();
            ViewBag.KeyId = KeyString(order);
            PopulateDropdowns(order);
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Order form)
        {
            var key = ConvertId(id);
            var order = await _context.Orders.FindAsync(key);
            if (order == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.KeyId = KeyString(order);
                PopulateDropdowns(form);
                return View(form);
            }

            // Copy editable properties except the key
            var (keyName, _) = KeyMeta();
            foreach (var p in typeof(Order).GetProperties().Where(p => p.CanRead && p.CanWrite))
            {
                if (string.Equals(p.Name, keyName, StringComparison.OrdinalIgnoreCase)) continue;
                p.SetValue(order, p.GetValue(form));
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ----- Delete -----
        public async Task<IActionResult> Delete(string id)
        {
            var key = ConvertId(id);
            var order = await _context.Orders.FindAsync(key);
            if (order == null) return NotFound();
            ViewBag.KeyId = KeyString(order);
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var key = ConvertId(id);
            var order = await _context.Orders.FindAsync(key);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
