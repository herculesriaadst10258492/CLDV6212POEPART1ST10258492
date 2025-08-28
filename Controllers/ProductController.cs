// File: Controllers/ProductController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ABCRetail;
using ABCRetail.Models;
using ABCRetail.Service;

namespace ABCRetail.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAzureStorageService _storage;

        public ProductController(ApplicationDbContext context, IAzureStorageService storage)
        {
            _context = context;
            _storage = storage;
        }

        private string KeyName()
        {
            var et = _context.Model.FindEntityType(typeof(Product));
            var pk = et?.FindPrimaryKey();
            return pk?.Properties.First().Name ?? "Id";
        }

        private int KeyValue(Product p)
        {
            var name = KeyName();
            var val = _context.Entry(p).Property(name).CurrentValue!;
            return Convert.ToInt32(val);
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.ToListAsync();
            var map = new Dictionary<int, Product>();
            foreach (var p in products) map[KeyValue(p)] = p;
            ViewBag.ProductMap = map;
            return View(products);
        }

        public IActionResult Create() => View(new Product());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? ImageFile)
        {
            // Upload first so ImageUrl validation won't block
            if (ImageFile is { Length: > 0 })
            {
                product.ImageUrl = await _storage.UploadBlobAsync(ImageFile);
                ModelState.Clear();
                TryValidateModel(product);
            }

            if (!ModelState.IsValid) return View(product);

            _context.Add(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            ViewBag.KeyId = id;
            return View(product);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            ViewBag.KeyId = id;
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product form, IFormFile? ImageFile)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Name = form.Name;
            product.Description = form.Description;
            product.Price = form.Price;

            if (ImageFile is { Length: > 0 })
            {
                product.ImageUrl = await _storage.UploadBlobAsync(ImageFile);
            }

            ModelState.Clear();
            TryValidateModel(product);
            if (!ModelState.IsValid)
            {
                ViewBag.KeyId = id;
                return View(form);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            ViewBag.KeyId = id;
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
