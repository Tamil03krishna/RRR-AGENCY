using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010008)]
    public class CreateInvoicesTable : Migration
    {
        public override void Up()
        {
            Create.Table("Invoices")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("InvoiceNo").AsString(30).NotNullable().Unique()  // e.g. INV-2026-00001
                .WithColumn("CustomerId").AsInt32().NotNullable().ForeignKey("Customers", "Id")
                .WithColumn("InvoiceDate").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("SubTotal").AsDecimal(12, 2).NotNullable()             // sum of item amounts (this invoice only)
                .WithColumn("PreviousBalance").AsDecimal(12, 2).NotNullable().WithDefaultValue(0) // carried forward from earlier unpaid/partial invoices
                .WithColumn("GrandTotal").AsDecimal(12, 2).NotNullable()           // SubTotal + PreviousBalance
                .WithColumn("PaidAmount").AsDecimal(12, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("BalanceAmount").AsDecimal(12, 2).NotNullable().WithDefaultValue(0) // GrandTotal - PaidAmount, rolls to next invoice
                .WithColumn("PaymentStatus").AsString(20).NotNullable()            // Paid, Partial, Unpaid
                .WithColumn("PaymentModeId").AsInt32().Nullable().ForeignKey("PaymentModes", "Id")
                .WithColumn("CreatedByUserId").AsInt32().Nullable().ForeignKey("Users", "Id")
                .WithColumn("IsCancelled").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("CreatedDate").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

            Create.Index("IX_Invoices_CustomerId").OnTable("Invoices").OnColumn("CustomerId");
            Create.Index("IX_Invoices_InvoiceDate").OnTable("Invoices").OnColumn("InvoiceDate");
        }

        public override void Down()
        {
            Delete.Table("Invoices");
        }
    }
}
