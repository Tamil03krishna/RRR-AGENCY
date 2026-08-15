using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010002)]
    public class CreateCustomersTable : Migration
    {
        public override void Up()
        {
            Create.Table("Customers")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString(150).NotNullable()
                .WithColumn("Phone").AsString(20).NotNullable()
                .WithColumn("Address").AsString(300).Nullable()
                .WithColumn("Email").AsString(150).Nullable()
                // running balance the customer owes the shop; billing module reads/updates this
                .WithColumn("OutstandingBalance").AsDecimal(12, 2).NotNullable().WithDefaultValue(0)
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedDate").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("UpdatedDate").AsDateTime().Nullable();

            Create.Index("IX_Customers_Phone")
                .OnTable("Customers")
                .OnColumn("Phone");

            Create.Index("IX_Customers_Name")
                .OnTable("Customers")
                .OnColumn("Name");
        }

        public override void Down()
        {
            Delete.Table("Customers");
        }
    }
}
