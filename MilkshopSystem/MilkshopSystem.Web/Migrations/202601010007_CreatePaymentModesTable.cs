using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010007)]
    public class CreatePaymentModesTable : Migration
    {
        public override void Up()
        {
            Create.Table("PaymentModes")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString(50).NotNullable().Unique() // Cash, GPay, Net Banking
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedDate").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

            Insert.IntoTable("PaymentModes").Row(new { Name = "Cash" });
            Insert.IntoTable("PaymentModes").Row(new { Name = "GPay" });
            Insert.IntoTable("PaymentModes").Row(new { Name = "Net Banking" });
        }

        public override void Down()
        {
            Delete.Table("PaymentModes");
        }
    }
}
