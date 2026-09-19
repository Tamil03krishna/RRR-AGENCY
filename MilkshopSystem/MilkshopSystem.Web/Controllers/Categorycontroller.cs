using Microsoft.AspNetCore.Mvc;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Repositories.Interfaces;
using MilkshopSystem.Web.Security;

namespace MilkshopSystem.Web.Controllers
{
    [ModuleAccess("Category")]
    public class CategoryController : BaseController
    {
        private readonly ICategoryRepository _repo;
        public CategoryController(ICategoryRepository repo) => _repo = repo;

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var result = await _repo.GetPagedAsync(search, page, pageSize);
            return View(result);
        }

        public IActionResult Create() => View(new ProductCategory());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCategory category)
        {
            if (!ModelState.IsValid) return View(category);
            await _repo.CreateAsync(category);
            TempData["Success"] = "Category added successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _repo.GetByIdAsync(id);
            if (category is null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductCategory category)
        {
            if (!ModelState.IsValid) return View(category);
            await _repo.UpdateAsync(category);
            TempData["Success"] = "Category updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            TempData["Success"] = "Category deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}