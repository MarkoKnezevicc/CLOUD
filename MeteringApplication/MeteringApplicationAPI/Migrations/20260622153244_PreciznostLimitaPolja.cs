using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeteringApplicationAPI.Migrations
{
    /// <inheritdoc />
    public partial class PreciznostLimitaPolja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PocetnoStanjeMeseca",
                table: "PametnaBrojila",
                type: "decimal(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PocetnoStanjeMeseca",
                table: "PametnaBrojila",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");
        }
    }
}
