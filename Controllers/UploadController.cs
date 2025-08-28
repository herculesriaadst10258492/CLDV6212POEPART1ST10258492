using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ABCRetail.Service;

namespace ABCRetail.Controllers
{
    public class UploadController : Controller
    {
        private readonly IAzureStorageService _storage;
        public UploadController(IAzureStorageService storage) => _storage = storage;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var urls = await _storage.ListBlobUrlsAsync();
            return View(urls); // model: IEnumerable<string> (URLs)
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(IFormFile? file)
        {
            if (file is { Length: > 0 })
                await _storage.UploadBlobAsync(file);
            return RedirectToAction(nameof(Index));
        }
    }
}
