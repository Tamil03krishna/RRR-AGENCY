using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010005)]
    public class CreateProductsTable : Migration
    {
        public override void Up()
        {
            Create.Table("Products")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString(150).NotNullable()
                .WithColumn("CategoryId").AsInt32().NotNullable().ForeignKey("ProductCategories", "Id")
                .WithColumn("UnitId").AsInt32().NotNullable().ForeignKey("Units", "Id")
                .WithColumn("Size").AsString(20).NotNullable()          // e.g. 500, 1000 (in the selected unit)
                .WithColumn("StorePrice").AsDecimal(12, 2).NotNullable() // shop's cost/selling price
                .WithColumn("MrpPrice").AsDecimal(12, 2).NotNullable()   // printed MRP
                .WithColumn("ActiveFrom").AsDate().NotNullable()
                .WithColumn("ActiveTo").AsDate().Nullable()              // null = still active, no end date
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedDate").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("UpdatedDate").AsDateTime().Nullable();

            Create.Index("IX_Products_Name")
                .OnTable("Products")
                .OnColumn("Name");
        }

        public override void Down()
        {
            Delete.Table("Products");
        }
    }
}
