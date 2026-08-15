using Microsoft.AspNetCore.Mvc;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Controllers
{
    public class PaymentModeController : BaseController
    {
        private readonly IPaymentModeRepository _repo;
        public PaymentModeController(IPaymentModeRepository repo) => _repo = repo;

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var result = await _repo.GetPagedAsync(search, page, pageSize);
            return View(result);
        }

        public IActionResult Create() => View(new PaymentMode());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentMode mode)
        {
            if (!ModelState.IsValid) return View(mode);
            await _repo.CreateAsync(mode);
            TempData["Success"] = "Payment mode add ஆயிடுச்சு.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var mode = await _repo.GetByIdAsync(id);
            if (mode is null) return NotFound();
            return View(mode);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PaymentMode mode)
        {
            if (!ModelState.IsValid) return View(mode);
            await _repo.UpdateAsync(mode);
            TempData["Success"] = "Payment mode update ஆயிடுச்சு.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            TempData["Success"] = "Payment mode delete ஆயிடுச்சு.";
            return RedirectToAction(nameof(Index));
        }
    }
}
