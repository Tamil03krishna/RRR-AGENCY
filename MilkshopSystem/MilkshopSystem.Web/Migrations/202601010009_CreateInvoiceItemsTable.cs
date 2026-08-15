using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010009)]
    public class CreateInvoiceItemsTable : Migration
    {
        public override void Up()
        {
            Create.Table("InvoiceItems")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("InvoiceId").AsInt32().NotNullable().ForeignKey("Invoices", "Id")
                .WithColumn("ProductId").AsInt32().NotNullable().ForeignKey("Products", "Id")
                .WithColumn("PriceType").AsString(20).NotNullable()   // "StorePrice" or "MrpPrice" - which one was picked
                .WithColumn("UnitPrice").AsDecimal(12, 2).NotNullable()
                .WithColumn("Qty").AsDecimal(12, 2).NotNullable()
                .WithColumn("Amount").AsDecimal(12, 2).NotNullable()  // UnitPrice * Qty, auto-calculated
                .WithColumn("CreatedDate").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

            Create.Index("IX_InvoiceItems_InvoiceId").OnTable("InvoiceItems").OnColumn("InvoiceId");
        }

        public override void Down()
        {
            Delete.Table("InvoiceItems");
        }
    }
}
