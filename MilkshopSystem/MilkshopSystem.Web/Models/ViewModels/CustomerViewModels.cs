using MilkshopSystem.Web.Models.Entities;

namespace MilkshopSystem.Web.Models.ViewModels
{
    public class OutstandingReportViewModel
    {
        // Customers who still owe money (OutstandingBalance > 0)
        public List<Customer> DueCustomers { get; set; } = new();

        // Customers who paid extra (OutstandingBalance < 0) — this credit
        // automatically reduces their next bill
        public List<Customer> AdvanceCustomers { get; set; } = new();

        public decimal TotalDue { get; set; }
        public decimal TotalAdvance { get; set; }

        // What the shop is net owed once advances are set off against dues
        public decimal NetOutstanding => TotalDue - TotalAdvance;
    }
}
