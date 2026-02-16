using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sunrise.Shared.Database.Migrations
{
    public partial class AddClanDescriptionAndNameChangedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "clan",
                type: "varchar(2048)",
                maxLength: 2048,
                nullable: true)
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "NameChangedAt",
                table: "clan",
                type: "datetime(6)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "clan");

            migrationBuilder.DropColumn(
                name: "NameChangedAt",
                table: "clan");
        }
    }
}
