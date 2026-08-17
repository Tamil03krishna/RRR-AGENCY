using Microsoft.AspNetCore.Mvc;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Controllers
{
    public class DashboardController : BaseController
    {
        private readonly IDashboardRepository _dashboardRepo;
        private readonly IStockRepository _stockRepo;

        public DashboardController(IDashboardRepository dashboardRepo, IStockRepository stockRepo)
        {
            _dashboardRepo = dashboardRepo;
            _stockRepo = stockRepo;
        }

        public async Task<IActionResult> Index()
        {
            var summary = await _dashboardRepo.GetSummaryAsync();
            ViewBag.LowStockItems = await _stockRepo.GetLowStockAsync();
            return View(summary);
        }
    }
}
