using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MilkshopSystem.Web.Models.ViewModels
{
    public class BillingItemInput
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public string PriceType { get; set; } = "StorePrice"; // StorePrice | MrpPrice

        [Range(0.01, double.MaxValue, ErrorMessage = "Qty 0 kum jaasthi irukanum")]
        public decimal Qty { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public decimal Amount => UnitPrice * Qty;

        // display-only, filled by JS from product search results
        public string? ProductName { get; set; }
        public string? Size { get; set; }
        public string? UnitSymbol { get; set; }
    }

    public class BillingCreateViewModel
    {
        public int? CustomerId { get; set; }

        [Required(ErrorMessage = "Customer name ")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number ")]
        public string CustomerPhone { get; set; } = string.Empty;

        public decimal PreviousBalance { get; set; } 

        public List<BillingItemInput> Items { get; set; } = new();

        [Required]
        public int PaymentModeId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; }

        public List<SelectListItem> PaymentModes { get; set; } = new();
    }
}
