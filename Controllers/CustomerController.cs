using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ABCRetail.Models;

namespace ABCRetail.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CustomerController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Customer
        public async Task<IActionResult> Index()
        {
            var customers = await _db.Customers
                                     .OrderBy(c => c.Name)
                                     .ToListAsync();
            return View(customers);
        }

        // GET: /Customer/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var entity = await _db.Customers.FindAsync(ConvertId(id));
            if (entity == null) return NotFound();

            return View(entity);
        }

        // GET: /Customer/Create
        public IActionResult Create() => View();

        // POST: /Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Phone,Address")] Customer customer)
        {
            if (!ModelState.IsValid) return View(customer);

            // Id is generated in the model (string Guid)
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Customer/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var entity = await _db.Customers.FindAsync(ConvertId(id));
            if (entity == null) return NotFound();

            return View(entity);
        }

        // POST: /Customer/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Name,Email,Phone,Address")] Customer model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var entity = await _db.Customers.FindAsync(ConvertId(id));
            if (entity == null) return NotFound();

            entity.Name = model.Name;
            entity.Email = model.Email;
            entity.Phone = model.Phone;
            entity.Address = model.Address;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Customer/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var entity = await _db.Customers.FindAsync(ConvertId(id));
            if (entity == null) return NotFound();

            return View(entity);
        }

        // POST: /Customer/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var entity = await _db.Customers.FindAsync(ConvertId(id));
            if (entity != null)
            {
                _db.Customers.Remove(entity);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // One and only ConvertId — resolves the PK type from EF and converts the route "id".
        private object ConvertId(string id)
        {
            var keyType = _db.Model.FindEntityType(typeof(Customer))!
                                   .FindPrimaryKey()!
                                   .Properties[0]
                                   .ClrType;

            if (keyType == typeof(int)) return int.Parse(id);
            if (keyType == typeof(Guid)) return Guid.Parse(id);
            return id; // string PK (our case)
        }
    }
}
