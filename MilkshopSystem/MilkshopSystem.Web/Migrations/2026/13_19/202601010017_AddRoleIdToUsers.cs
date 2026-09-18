using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010017)]
    public class AddRoleIdToUsers : Migration
    {
        public override void Up()
        {
            Alter.Table("Users")
                .AddColumn("RoleId").AsInt32().Nullable();

            Create.ForeignKey("FK_Users_Roles")
                .FromTable("Users").ForeignColumn("RoleId")
                .ToTable("Roles").PrimaryColumn("Id");

            // Point the existing seeded admin user at the Admin role
            Execute.Sql("UPDATE Users SET RoleId = (SELECT Id FROM Roles WHERE Name = 'Admin') WHERE Role = 'Admin'");
        }

        public override void Down()
        {
            Delete.ForeignKey("FK_Users_Roles").OnTable("Users");
            Delete.Column("RoleId").FromTable("Users");
        }
    }
}
