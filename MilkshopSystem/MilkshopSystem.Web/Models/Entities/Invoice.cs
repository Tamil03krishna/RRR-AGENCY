namespace MilkshopSystem.Web.Models.Entities
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal PreviousBalance { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid"; // Paid | Partial | Unpaid
        public int? PaymentModeId { get; set; }
        public int? CreatedByUserId { get; set; }
        public bool IsCancelled { get; set; }
        public DateTime CreatedDate { get; set; }

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? PaymentModeName { get; set; }
        public List<InvoiceItem> Items { get; set; } = new();
    }

    public class InvoiceItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public int ProductId { get; set; }
        public string PriceType { get; set; } = "StorePrice"; // StorePrice | MrpPrice
        public decimal UnitPrice { get; set; }
        public decimal Qty { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedDate { get; set; }

        public string? ProductName { get; set; }
        public string? Size { get; set; }
        public string? UnitSymbol { get; set; }
    }

    public class InvoicePayment
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public int PaymentModeId { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}