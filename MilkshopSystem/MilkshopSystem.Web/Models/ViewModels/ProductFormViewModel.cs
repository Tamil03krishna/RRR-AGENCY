using MilkshopSystem.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MilkshopSystem.Web.Models.ViewModels
{
    public class ProductFormViewModel
    {
        public Product Product { get; set; } = new();

        // only used on Create - initial opening stock + low stock threshold for the new product
        [Range(0, double.MaxValue)]
        public decimal OpeningStock { get; set; }

        [Range(0, double.MaxValue)]
        public decimal LowStockLevel { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Units { get; set; } = new();
    }
}
