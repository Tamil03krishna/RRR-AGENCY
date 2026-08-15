using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010001)]
    public class CreateUsersTable : Migration
    {
        public override void Up()
        {
            Create.Table("Users")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Username").AsString(50).NotNullable().Unique()
                .WithColumn("PasswordHash").AsString(255).NotNullable()
                .WithColumn("FullName").AsString(100).NotNullable()
                .WithColumn("Role").AsString(30).NotNullable().WithDefaultValue("Admin")
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedDate").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("UpdatedDate").AsDateTime().Nullable();
        }

        public override void Down()
        {
            Delete.Table("Users");
        }
    }
}
