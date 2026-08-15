namespace MilkshopSystem.Web.Models.Entities
{
    public class PaymentMode
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Cash, GPay, Net Banking
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
