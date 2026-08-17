using Microsoft.AspNetCore.Mvc;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Controllers
{
    public class CustomerController : BaseController
    {
        private readonly ICustomerRepository _repo;
        public CustomerController(ICustomerRepository repo) => _repo = repo;

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var result = await _repo.GetPagedAsync(search, page, pageSize);
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> SearchJson(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Json(new List<Customer>());
            var results = await _repo.SearchAsync(term, 10);
            return Json(results);
        }

        public IActionResult Create() => View(new Customer());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!ModelState.IsValid) return View(customer);
            await _repo.CreateAsync(customer);
            TempData["Success"] = "Customer add ஆயிடுச்சு.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _repo.GetByIdAsync(id);
            if (customer is null) return NotFound();
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (!ModelState.IsValid) return View(customer);
            await _repo.UpdateAsync(customer);
            TempData["Success"] = "Customer update ஆயிடுச்சு.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _repo.GetByIdAsync(id);
            if (customer is null) return NotFound();
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            TempData["Success"] = "Customer delete ஆயிடுச்சு.";
            return RedirectToAction(nameof(Index));
        }
    }
}
