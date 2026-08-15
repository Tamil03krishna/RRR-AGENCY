using FluentMigrator;

namespace MilkshopSystem.Web.Migrations
{
    [Migration(202601010003)]
    public class CreateUnitsTable : Migration
    {
        public override void Up()
        {
            Create.Table("Units")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString(50).NotNullable().Unique()   // e.g. Kilogram, Gram, Liter, Packet
                .WithColumn("Symbol").AsString(10).NotNullable()          // e.g. kg, g, L, pkt
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedDate").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("UpdatedDate").AsDateTime().Nullable();

            Insert.IntoTable("Units").Row(new { Name = "Kilogram", Symbol = "kg" });
            Insert.IntoTable("Units").Row(new { Name = "Gram", Symbol = "g" });
            Insert.IntoTable("Units").Row(new { Name = "Liter", Symbol = "L" });
            Insert.IntoTable("Units").Row(new { Name = "Packet", Symbol = "pkt" });
        }

        public override void Down()
        {
            Delete.Table("Units");
        }
    }
}
