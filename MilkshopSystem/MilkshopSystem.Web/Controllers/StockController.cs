using Microsoft.AspNetCore.Mvc;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Controllers
{
    public class StockController : BaseController
    {
        private readonly IStockRepository _repo;
        public StockController(IStockRepository repo) => _repo = repo;

        // point 5 + point 10: list with product name, size, unit, opening/current stock, low-stock flag, search + pagination
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var result = await _repo.GetPagedAsync(search, page, pageSize);
            return View(result);
        }

        // billing screen calls this when a product is picked, to show "stock qty evlo iruku" live (point 6)
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
            // id here is the Stock.Id - fetched through product's stock row
            var stock = await _repo.GetByProductIdAsync(id);
            if (stock is null) return NotFound();
            return View(stock);
        }

        // manual stock adjustment (e.g. new delivery arrived, correction) - separate from billing auto-deduction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Stock stock)
        {
            if (!ModelState.IsValid) return View(stock);
            await _repo.UpdateAsync(stock);
            TempData["Success"] = "Stock update ஆயிடுச்சு.";
            return RedirectToAction(nameof(Index));
        }
    }
}
