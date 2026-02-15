using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Sunrise.Shared.Database.Migrations
{
    public partial class AddClansMvp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySQL:Charset", "utf8mb4"),
                    AvatarUrl = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySQL:Charset", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "clan_member",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ClanId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan_member", x => x.Id);
                    table.ForeignKey(
                        name: "FK_clan_member_clan_ClanId",
                        column: x => x.ClanId,
                        principalTable: "clan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_clan_member_user_UserId",
                        column: x => x.UserId,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ClanId",
                table: "user",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_clan_Name",
                table: "clan",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clan_member_ClanId_UserId",
                table: "clan_member",
                columns: new[] { "ClanId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clan_member_UserId",
                table: "clan_member",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_ClanId",
                table: "user",
                column: "ClanId");

            migrationBuilder.AddForeignKey(
                name: "FK_user_clan_ClanId",
                table: "user",
                column: "ClanId",
                principalTable: "clan",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_clan_ClanId",
                table: "user");

            migrationBuilder.DropTable(
                name: "clan_member");

            migrationBuilder.DropTable(
                name: "clan");

            migrationBuilder.DropIndex(
                name: "IX_user_ClanId",
                table: "user");

            migrationBuilder.DropColumn(
                name: "ClanId",
                table: "user");
        }
    }
}
