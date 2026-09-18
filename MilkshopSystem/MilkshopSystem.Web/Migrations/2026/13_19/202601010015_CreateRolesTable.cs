using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010015)]
    public class CreateRolesTable : Migration
    {
        public override void Up()
        {
            Create.Table("Roles")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString(50).NotNullable().Unique()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedDate").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("UpdatedDate").AsDateTime().Nullable();
        }

        public override void Down()
        {
            Delete.Table("Roles");
        }
    }
}
