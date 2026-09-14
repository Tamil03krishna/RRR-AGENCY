namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public class DashboardSummary
    {
        public decimal TodaySales { get; set; }
        public decimal MonthSales { get; set; }
        public decimal TotalStockValue { get; set; }
        public int LowStockCount { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int TotalProducts { get; set; }
        public int CurrentStock{ get; set; }
    }

    public interface IDashboardRepository
    {
        Task<DashboardSummary> GetSummaryAsync();
    }
}
