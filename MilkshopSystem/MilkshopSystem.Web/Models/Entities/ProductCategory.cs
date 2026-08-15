namespace MilkshopSystem.Web.Models.Entities
{
    public class ProductCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Milk Products, Tea, Snacks
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
