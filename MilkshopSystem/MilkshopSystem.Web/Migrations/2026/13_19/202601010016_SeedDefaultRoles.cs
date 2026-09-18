using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010016)]
    public class SeedDefaultRoles : Migration
    {
        public override void Up()
        {
            Insert.IntoTable("Roles").Row(new { Name = "Admin", IsActive = true });
            Insert.IntoTable("Roles").Row(new { Name = "Cashier", IsActive = true });
            Insert.IntoTable("Roles").Row(new { Name = "Staff", IsActive = true });
        }

        public override void Down()
        {
            Delete.FromTable("Roles").Row(new { Name = "Admin" });
            Delete.FromTable("Roles").Row(new { Name = "Cashier" });
            Delete.FromTable("Roles").Row(new { Name = "Staff" });
        }
    }
}
