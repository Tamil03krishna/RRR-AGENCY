using Microsoft.AspNetCore.Mvc;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Controllers
{
    public class ReportController : BaseController
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IProductRepository _productRepo;

        public ReportController(IInvoiceRepository invoiceRepo, IProductRepository productRepo)
        {
            _invoiceRepo = invoiceRepo;
            _productRepo = productRepo;
        }

        public IActionResult Charts() => View();

        [HttpGet]
        public async Task<IActionResult> MonthlyEarningsJson(int? year, int? productId)
        {
            year ??= DateTime.Now.Year;
            var data = await _invoiceRepo.GetMonthlyEarningsAsync(year.Value, productId);
            return Json(data.Select(d => new { month = d.Month, total = d.Total }));
        }

        [HttpGet]
        public async Task<IActionResult> WeeklyEarningsJson(DateTime? from, DateTime? to)
        {
            from ??= DateTime.Today.AddDays(-56); 
            to ??= DateTime.Today;
            var data = await _invoiceRepo.GetWeeklyEarningsAsync(from.Value, to.Value);
            return Json(data.Select(d => new { weekStart = d.WeekStart.ToString("yyyy-MM-dd"), total = d.Total }));
        }

        [HttpGet]
        public async Task<IActionResult> YearlyEarningsJson()
        {
            var data = await _invoiceRepo.GetYearlyEarningsAsync();
            return Json(data.Select(d => new { year = d.Year, total = d.Total }));
        }

        [HttpGet]
        public async Task<IActionResult> ProductListJson()
        {
            var result = await _productRepo.GetPagedAsync(null, 1, 1000);
            return Json(result.Items.Select(p => new { id = p.Id, name = $"{p.Name} ({p.Size}{p.UnitSymbol})" }));
        }
    }
}
