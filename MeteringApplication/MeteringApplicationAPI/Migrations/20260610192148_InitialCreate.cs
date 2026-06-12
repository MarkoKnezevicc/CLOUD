using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeteringApplicationAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Korisnici",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prezime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LozinkaHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Uloga = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Korisnici", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Objekti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Grad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Adresa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KorisnikId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objekti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Objekti_Korisnici_KorisnikId",
                        column: x => x.KorisnikId,
                        principalTable: "Korisnici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PametnaBrojila",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OznakaBrojila = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SerijskiBroj = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Tip = table.Column<int>(type: "int", nullable: false),
                    MaksimalnaOdobrenaSnaga = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Napomena = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Uuid = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeviceAccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ObjekatId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PametnaBrojila", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PametnaBrojila_Objekti_ObjekatId",
                        column: x => x.ObjekatId,
                        principalTable: "Objekti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Korisnici_Email",
                table: "Korisnici",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Objekti_KorisnikId",
                table: "Objekti",
                column: "KorisnikId");

            migrationBuilder.CreateIndex(
                name: "IX_PametnaBrojila_ObjekatId",
                table: "PametnaBrojila",
                column: "ObjekatId");

            migrationBuilder.CreateIndex(
                name: "IX_PametnaBrojila_SerijskiBroj",
                table: "PametnaBrojila",
                column: "SerijskiBroj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PametnaBrojila_Uuid",
                table: "PametnaBrojila",
                column: "Uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PametnaBrojila");

            migrationBuilder.DropTable(
                name: "Objekti");

            migrationBuilder.DropTable(
                name: "Korisnici");
        }
    }
}
