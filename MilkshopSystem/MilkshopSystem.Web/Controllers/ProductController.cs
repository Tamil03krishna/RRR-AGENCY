using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MilkshopSystem.Web.Models.ViewModels;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Controllers
{
    public class ProductController : BaseController
    {
        private readonly IProductRepository _productRepo;
        private readonly IUnitRepository _unitRepo;
        private readonly IStockRepository _stockRepo;

        public ProductController(IProductRepository productRepo, IUnitRepository unitRepo, IStockRepository stockRepo)
        {
            _productRepo = productRepo;
            _unitRepo = unitRepo;
            _stockRepo = stockRepo;
        }

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var result = await _productRepo.GetPagedAsync(search, page, pageSize);
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> SearchJson(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return Json(new List<object>());
            var products = await _productRepo.SearchActiveAsync(term, 15);
            var result = products.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                size = p.Size,
                unitSymbol = p.UnitSymbol,
                storePrice = p.StorePrice,
                mrpPrice = p.MrpPrice
            });
            return Json(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product is null) return NotFound();
            var stock = await _stockRepo.GetByProductIdAsync(id);
            ViewBag.Stock = stock;
            return View(product);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new ProductFormViewModel
            {
                Product = new() { ActiveFrom = DateTime.Today },
                Categories = await GetCategoryOptions(),
                Units = await GetUnitOptions()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Categories = await GetCategoryOptions();
                vm.Units = await GetUnitOptions();
                return View(vm);
            }

            var productId = await _productRepo.CreateAsync(vm.Product);
            await _stockRepo.CreateForProductAsync(productId, vm.OpeningStock, vm.LowStockLevel);

            TempData["Success"] = "Product add successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product is null) return NotFound();

            var vm = new ProductFormViewModel
            {
                Product = product,
                Categories = await GetCategoryOptions(),
                Units = await GetUnitOptions()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Categories = await GetCategoryOptions();
                vm.Units = await GetUnitOptions();
                return View(vm);
            }

            await _productRepo.UpdateAsync(vm.Product);
            TempData["Success"] = "Product update successfully";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _productRepo.DeleteAsync(id);
            TempData["Success"] = "Product delete sucessfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetCategoryOptions()
        {
            var categories = await _productRepo.GetCategoriesAsync();
            return categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
        }

        private async Task<List<SelectListItem>> GetUnitOptions()
        {
            var units = await _unitRepo.GetAllActiveAsync();
            return units.Select(u => new SelectListItem($"{u.Name} ({u.Symbol})", u.Id.ToString())).ToList();
        }
    }
}
