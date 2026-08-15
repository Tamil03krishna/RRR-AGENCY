using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010010)]
    public class CreateInvoicePaymentsTable : Migration
    {
        public override void Up()
        {
            // Tracks every payment received against an invoice.
            // A "Partial" invoice can get more InvoicePayments rows added later
            // (e.g. next time the customer comes in and pays off some balance),
            // without needing a whole new invoice.
            Create.Table("InvoicePayments")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("InvoiceId").AsInt32().NotNullable().ForeignKey("Invoices", "Id")
                .WithColumn("CustomerId").AsInt32().NotNullable().ForeignKey("Customers", "Id")
                .WithColumn("Amount").AsDecimal(12, 2).NotNullable()
                .WithColumn("PaymentModeId").AsInt32().NotNullable().ForeignKey("PaymentModes", "Id")
                .WithColumn("PaymentDate").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

            Create.Index("IX_InvoicePayments_InvoiceId").OnTable("InvoicePayments").OnColumn("InvoiceId");
            Create.Index("IX_InvoicePayments_CustomerId").OnTable("InvoicePayments").OnColumn("CustomerId");
        }

        public override void Down()
        {
            Delete.Table("InvoicePayments");
        }
    }
}
