using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010018)]
    public class CreateUserPermissionsTable : Migration
    {
        public override void Up()
        {
            Create.Table("UserPermissions")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("ModuleName").AsString(50).NotNullable()
                .WithColumn("CanView").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("CanAdd").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("CanEdit").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("CanDelete").AsBoolean().NotNullable().WithDefaultValue(false);

            Create.ForeignKey("FK_UserPermissions_Users")
                .FromTable("UserPermissions").ForeignColumn("UserId")
                .ToTable("Users").PrimaryColumn("Id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.Index("IX_UserPermissions_UserId_ModuleName")
                .OnTable("UserPermissions")
                .OnColumn("UserId").Ascending()
                .OnColumn("ModuleName").Ascending()
                .WithOptions().Unique();
        }

        public override void Down()
        {
            Delete.Table("UserPermissions");
        }
    }
}
