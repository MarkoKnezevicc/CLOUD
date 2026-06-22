using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeteringApplicationAPI.Migrations
{
    /// <inheritdoc />
    public partial class DropOznakaBrojilaColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OznakaBrojila",
                table: "PametnaBrojila");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OznakaBrojila",
                table: "PametnaBrojila",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
