using Microsoft.AspNetCore.Mvc;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Controllers
{
    public class StockController : BaseController
    {
        private readonly IStockRepository _repo;
        public StockController(IStockRepository repo) => _repo = repo;

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var result = await _repo.GetPagedAsync(search, page, pageSize);
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableQty(int productId)
        {
            var stock = await _repo.GetByProductIdAsync(productId);
            return Json(new { availableQty = stock?.CurrentStock ?? 0 });
        }

        public async Task<IActionResult> LowStock()
        {
            var lowStock = await _repo.GetLowStockAsync();
            return View(lowStock);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var stock = await _repo.GetByProductIdAsync(id);
            if (stock is null) return NotFound();
            return View(stock);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Stock stock)
        {
            if (!ModelState.IsValid) return View(stock);
            await _repo.UpdateAsync(stock);
            TempData["Success"] = "Stock update sucessfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
