using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010004)]
    public class CreateProductCategoriesTable : Migration
    {
        public override void Up()
        {
            Create.Table("ProductCategories")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString(50).NotNullable().Unique() // Milk Products, Tea, Snacks
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedDate").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

            Insert.IntoTable("ProductCategories").Row(new { Name = "Milk Products" });
            Insert.IntoTable("ProductCategories").Row(new { Name = "Tea" });
            Insert.IntoTable("ProductCategories").Row(new { Name = "Snacks" });
        }

        public override void Down()
        {
            Delete.Table("ProductCategories");
        }
    }
}
