namespace MilkshopSystem.Web.Models.Entities
{
    public class Stock
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public decimal OpeningStock { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal LowStockLevel { get; set; }
        public DateTime LastUpdated { get; set; }

        // joined display fields
        public string? ProductName { get; set; }
        public string? Size { get; set; }
        public string? UnitSymbol { get; set; }
        public bool IsLowStock => CurrentStock <= LowStockLevel;
    }
}
