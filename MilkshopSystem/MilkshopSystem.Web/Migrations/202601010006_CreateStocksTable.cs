using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010006)]
    public class CreateStocksTable : Migration
    {
        public override void Up()
        {
            Create.Table("Stocks")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ProductId").AsInt32().NotNullable().ForeignKey("Products", "Id").Unique()
                .WithColumn("OpeningStock").AsDecimal(12, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("CurrentStock").AsDecimal(12, 2).NotNullable().WithDefaultValue(0) // reduced automatically on billing
                .WithColumn("LowStockLevel").AsDecimal(12, 2).NotNullable().WithDefaultValue(0) // threshold to flag "low stock"
                .WithColumn("LastUpdated").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);
        }

        public override void Down()
        {
            Delete.Table("Stocks");
        }
    }
}
