using Microsoft.AspNetCore.Mvc;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Controllers
{
    public class UnitController : BaseController
    {
        private readonly IUnitRepository _repo;
        public UnitController(IUnitRepository repo) => _repo = repo;

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var result = await _repo.GetPagedAsync(search, page, pageSize);
            return View(result);
        }

        public IActionResult Create() => View(new Unit());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Unit unit)
        {
            if (!ModelState.IsValid) return View(unit);
            await _repo.CreateAsync(unit);
            TempData["Success"] = "Unit add successfully    .";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var unit = await _repo.GetByIdAsync(id);
            if (unit is null) return NotFound();
            return View(unit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Unit unit)
        {
            if (!ModelState.IsValid) return View(unit);
            await _repo.UpdateAsync(unit);
            TempData["Success"] = "Unit update ஆயிடுச்சு.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            TempData["Success"] = "Unit delete ஆயிடுச்சு.";
            return RedirectToAction(nameof(Index));
        }
    }
}
