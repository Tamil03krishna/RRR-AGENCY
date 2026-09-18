using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010013)]
    public class AddBarcodeToProducts : Migration
    {
        public override void Up()
        {
            Alter.Table("Products")
                .AddColumn("Barcode").AsString(50).Nullable();

            Create.Index("IX_Products_Barcode")
                .OnTable("Products")
                .OnColumn("Barcode")
                .Unique();
        }

        public override void Down()
        {
            Delete.Index("IX_Products_Barcode").OnTable("Products");
            Delete.Column("Barcode").FromTable("Products");
        }
    }
}