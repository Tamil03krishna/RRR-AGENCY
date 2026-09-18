namespace MilkshopSystem.Web.Models.Entities
{
    public class UserPermission
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    // The fixed list of modules an Admin can grant access to.
    // Add a new module name here whenever a new manageable module is built.
    public static class AppModules
    {
        public static readonly string[] All =
        {
            "Customer",
            "Product",
            "Category",
            "Unit",
            "Billing",
            "Stock",
            "PaymentMode",
            "Notes"
        };
    }
}
