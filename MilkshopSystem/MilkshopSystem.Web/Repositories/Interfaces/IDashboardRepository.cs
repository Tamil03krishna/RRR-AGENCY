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

        public List<RecentInvoiceItem> RecentInvoices { get; set; } = new();
        public List<RecentPaymentItem> RecentPayments { get; set; } = new();
    }

    public class RecentInvoiceItem
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
    }

    public class RecentPaymentItem
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentModeName { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
    }

    public interface IDashboardRepository
    {
        Task<DashboardSummary> GetSummaryAsync();
    }
}