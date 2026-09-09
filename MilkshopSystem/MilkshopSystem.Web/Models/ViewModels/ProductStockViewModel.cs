namespace MilkshopSystem.Web.Models.ViewModels
{
    public class ProductStockViewModel
    {
        public int StockId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public bool IsActive { get; set; }
        public string Size { get; set; } = string.Empty;
        public string UnitSymbol { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal LowStockLevel { get; set; }
        public decimal StorePrice { get; set; }
        public decimal MrpPrice { get; set; }
        public decimal StockValue { get; set; }
    }
}
