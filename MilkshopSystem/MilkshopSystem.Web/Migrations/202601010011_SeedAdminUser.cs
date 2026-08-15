using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010011)]
    public class SeedAdminUser : Migration
    {
        public override void Up()
        {
            // Default login: username = admin, password = Admin@123
            // CHANGE THIS PASSWORD after first login in production.
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");

            Insert.IntoTable("Users").Row(new
            {
                Username = "admin",
                PasswordHash = passwordHash,
                FullName = "Shop Admin",
                Role = "Admin",
                IsActive = true
            });
        }

        public override void Down()
        {
            Delete.FromTable("Users").Row(new { Username = "admin" });
        }
    }
}
