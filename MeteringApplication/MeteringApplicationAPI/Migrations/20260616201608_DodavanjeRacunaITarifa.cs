using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeteringApplicationAPI.Migrations
{
    /// <inheritdoc />
    public partial class DodavanjeRacunaITarifa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PametnaBrojila_Uuid",
                table: "PametnaBrojila");

            migrationBuilder.AlterColumn<string>(
                name: "Uuid",
                table: "PametnaBrojila",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceAccessToken",
                table: "PametnaBrojila",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "Racuni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrojiloId = table.Column<int>(type: "int", nullable: false),
                    KorisnikId = table.Column<int>(type: "int", nullable: false),
                    GodinaObracuna = table.Column<int>(type: "int", nullable: false),
                    MesecObracuna = table.Column<int>(type: "int", nullable: false),
                    EnergijaVT = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EnergijaNT = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IznosZelena = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IznosPlava = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IznosCrvena = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FiksniTroskovi = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UkupanIznos = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DatumIzdavanja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TekstRacuna = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Racuni", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Racuni_Korisnici_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Racuni_PametnaBrojila_BrojiloId",
                        column: x => x.BrojiloId,
                        principalTable: "PametnaBrojila",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TarifniModeli",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CenaZ_VT = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CenaZ_NT = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CenaP_VT = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CenaP_NT = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CenaC_VT = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CenaC_NT = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CenaObracunskeSnage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TrosakSnabdevaca = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DatumKreiranja = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAktivan = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarifniModeli", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PametnaBrojila_Uuid",
                table: "PametnaBrojila",
                column: "Uuid",
                unique: true,
                filter: "[Uuid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Racuni_BrojiloId",
                table: "Racuni",
                column: "BrojiloId");

            migrationBuilder.CreateIndex(
                name: "IX_Racuni_KorisnikId",
                table: "Racuni",
                column: "KorisnikId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Racuni");

            migrationBuilder.DropTable(
                name: "TarifniModeli");

            migrationBuilder.DropIndex(
                name: "IX_PametnaBrojila_Uuid",
                table: "PametnaBrojila");

            migrationBuilder.AlterColumn<string>(
                name: "Uuid",
                table: "PametnaBrojila",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceAccessToken",
                table: "PametnaBrojila",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PametnaBrojila_Uuid",
                table: "PametnaBrojila",
                column: "Uuid",
                unique: true);
        }
    }
}
