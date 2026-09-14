using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010012)]
    public class CreateNotesTable : Migration
    {
        public override void Up()
        {
            Create.Table("Notes")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt32().NotNullable()
                .WithColumn("Content").AsString(2000).NotNullable()
                .WithColumn("Color").AsString(20).NotNullable().WithDefaultValue("yellow")
                .WithColumn("IsPinned").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("CreatedDate").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("UpdatedDate").AsDateTime().Nullable();

            Create.Index("IX_Notes_UserId").OnTable("Notes").OnColumn("UserId");
        }

        public override void Down()
        {
            Delete.Table("Notes");
        }
    }
}