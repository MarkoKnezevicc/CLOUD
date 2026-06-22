using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeteringApplicationAPI.Migrations
{
    /// <inheritdoc />
    public partial class DodatoLimitPotrosnje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LimitJedinica",
                table: "PametnaBrojila",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LimitVrednost",
                table: "PametnaBrojila",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PocetnoStanjeMeseca",
                table: "PametnaBrojila",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PracenjeGodina",
                table: "PametnaBrojila",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PracenjeMesec",
                table: "PametnaBrojila",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UpozorenjePoslato",
                table: "PametnaBrojila",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LimitJedinica",
                table: "PametnaBrojila");

            migrationBuilder.DropColumn(
                name: "LimitVrednost",
                table: "PametnaBrojila");

            migrationBuilder.DropColumn(
                name: "PocetnoStanjeMeseca",
                table: "PametnaBrojila");

            migrationBuilder.DropColumn(
                name: "PracenjeGodina",
                table: "PametnaBrojila");

            migrationBuilder.DropColumn(
                name: "PracenjeMesec",
                table: "PametnaBrojila");

            migrationBuilder.DropColumn(
                name: "UpozorenjePoslato",
                table: "PametnaBrojila");
        }
    }
}
