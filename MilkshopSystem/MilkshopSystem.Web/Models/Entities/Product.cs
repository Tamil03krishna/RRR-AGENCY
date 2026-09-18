namespace MilkshopSystem.Web.Models.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public int UnitId { get; set; }
        public string Size { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal StorePrice { get; set; }
        public decimal MrpPrice { get; set; }
        public DateTime ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? CategoryName { get; set; }
        public string? UnitName { get; set; }
        public string? UnitSymbol { get; set; }
    }
}