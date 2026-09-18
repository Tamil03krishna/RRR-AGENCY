using FluentMigrator;

namespace MilkshopSystem.Web.Migrations._2026._13_19
{
    [Migration(202609171432)]
    public class AddUpdatedDateToProductCategories : Migration
    {
        public override void Up()
        {
            Alter.Table("ProductCategories")
                .AddColumn("UpdatedDate").AsDateTime().Nullable();
        }

        public override void Down()
        {
            Delete.Column("UpdatedDate").FromTable("ProductCategories");
        }
    }
}