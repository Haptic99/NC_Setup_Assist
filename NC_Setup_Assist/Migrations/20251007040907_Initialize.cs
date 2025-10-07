using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NC_Setup_Assist.Migrations
{
    /// <inheritdoc />
    public partial class Initialize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hersteller",
                columns: table => new
                {
                    HerstellerID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hersteller", x => x.HerstellerID);
                });

            migrationBuilder.CreateTable(
                name: "Standorte",
                columns: table => new
                {
                    StandortID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PLZ = table.Column<string>(type: "TEXT", nullable: false),
                    Stadt = table.Column<string>(type: "TEXT", nullable: false),
                    Strasse = table.Column<string>(type: "TEXT", nullable: false),
                    Hausnummer = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Standorte", x => x.StandortID);
                });

            migrationBuilder.CreateTable(
                name: "WerkzeugKategorien",
                columns: table => new
                {
                    WerkzeugKategorieID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WerkzeugKategorien", x => x.WerkzeugKategorieID);
                });

            migrationBuilder.CreateTable(
                name: "Maschinen",
                columns: table => new
                {
                    MaschineID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HerstellerID = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Seriennummer = table.Column<string>(type: "TEXT", nullable: true),
                    AnzahlStationen = table.Column<int>(type: "INTEGER", nullable: false),
                    StandortID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maschinen", x => x.MaschineID);
                    table.ForeignKey(
                        name: "FK_Maschinen_Hersteller_HerstellerID",
                        column: x => x.HerstellerID,
                        principalTable: "Hersteller",
                        principalColumn: "HerstellerID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Maschinen_Standorte_StandortID",
                        column: x => x.StandortID,
                        principalTable: "Standorte",
                        principalColumn: "StandortID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WerkzeugUnterkategorien",
                columns: table => new
                {
                    WerkzeugUnterkategorieID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    WerkzeugKategorieID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WerkzeugUnterkategorien", x => x.WerkzeugUnterkategorieID);
                    table.ForeignKey(
                        name: "FK_WerkzeugUnterkategorien_WerkzeugKategorien_WerkzeugKategorieID",
                        column: x => x.WerkzeugKategorieID,
                        principalTable: "WerkzeugKategorien",
                        principalColumn: "WerkzeugKategorieID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projekte",
                columns: table => new
                {
                    ProjektID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    MaschineID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projekte", x => x.ProjektID);
                    table.ForeignKey(
                        name: "FK_Projekte_Maschinen_MaschineID",
                        column: x => x.MaschineID,
                        principalTable: "Maschinen",
                        principalColumn: "MaschineID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Werkzeuge",
                columns: table => new
                {
                    WerkzeugID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                    WerkzeugUnterkategorieID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Werkzeuge", x => x.WerkzeugID);
                    table.ForeignKey(
                        name: "FK_Werkzeuge_WerkzeugUnterkategorien_WerkzeugUnterkategorieID",
                        column: x => x.WerkzeugUnterkategorieID,
                        principalTable: "WerkzeugUnterkategorien",
                        principalColumn: "WerkzeugUnterkategorieID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NCProgramme",
                columns: table => new
                {
                    NCProgrammID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ZeichnungsNummer = table.Column<string>(type: "TEXT", nullable: false),
                    Bezeichnung = table.Column<string>(type: "TEXT", nullable: false),
                    DateiPfad = table.Column<string>(type: "TEXT", nullable: false),
                    MaschineID = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjektID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NCProgramme", x => x.NCProgrammID);
                    table.ForeignKey(
                        name: "FK_NCProgramme_Maschinen_MaschineID",
                        column: x => x.MaschineID,
                        principalTable: "Maschinen",
                        principalColumn: "MaschineID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NCProgramme_Projekte_ProjektID",
                        column: x => x.ProjektID,
                        principalTable: "Projekte",
                        principalColumn: "ProjektID");
                });

            migrationBuilder.CreateTable(
                name: "StandardWerkzeugZuweisungen",
                columns: table => new
                {
                    StandardWerkzeugZuweisungID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RevolverStation = table.Column<int>(type: "INTEGER", nullable: false),
                    MaschineID = table.Column<int>(type: "INTEGER", nullable: false),
                    WerkzeugID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandardWerkzeugZuweisungen", x => x.StandardWerkzeugZuweisungID);
                    table.ForeignKey(
                        name: "FK_StandardWerkzeugZuweisungen_Maschinen_MaschineID",
                        column: x => x.MaschineID,
                        principalTable: "Maschinen",
                        principalColumn: "MaschineID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StandardWerkzeugZuweisungen_Werkzeuge_WerkzeugID",
                        column: x => x.WerkzeugID,
                        principalTable: "Werkzeuge",
                        principalColumn: "WerkzeugID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WerkzeugEinsaetze",
                columns: table => new
                {
                    WerkzeugEinsatzID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Reihenfolge = table.Column<int>(type: "INTEGER", nullable: false),
                    Anzahl = table.Column<int>(type: "INTEGER", nullable: false),
                    RevolverStation = table.Column<string>(type: "TEXT", nullable: true),
                    KorrekturNummer = table.Column<string>(type: "TEXT", nullable: true),
                    Kommentar = table.Column<string>(type: "TEXT", nullable: true),
                    NCProgrammID = table.Column<int>(type: "INTEGER", nullable: false),
                    WerkzeugID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WerkzeugEinsaetze", x => x.WerkzeugEinsatzID);
                    table.ForeignKey(
                        name: "FK_WerkzeugEinsaetze_NCProgramme_NCProgrammID",
                        column: x => x.NCProgrammID,
                        principalTable: "NCProgramme",
                        principalColumn: "NCProgrammID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WerkzeugEinsaetze_Werkzeuge_WerkzeugID",
                        column: x => x.WerkzeugID,
                        principalTable: "Werkzeuge",
                        principalColumn: "WerkzeugID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Maschinen_HerstellerID",
                table: "Maschinen",
                column: "HerstellerID");

            migrationBuilder.CreateIndex(
                name: "IX_Maschinen_StandortID",
                table: "Maschinen",
                column: "StandortID");

            migrationBuilder.CreateIndex(
                name: "IX_NCProgramme_MaschineID",
                table: "NCProgramme",
                column: "MaschineID");

            migrationBuilder.CreateIndex(
                name: "IX_NCProgramme_ProjektID",
                table: "NCProgramme",
                column: "ProjektID");

            migrationBuilder.CreateIndex(
                name: "IX_Projekte_MaschineID",
                table: "Projekte",
                column: "MaschineID");

            migrationBuilder.CreateIndex(
                name: "IX_StandardWerkzeugZuweisungen_MaschineID",
                table: "StandardWerkzeugZuweisungen",
                column: "MaschineID");

            migrationBuilder.CreateIndex(
                name: "IX_StandardWerkzeugZuweisungen_WerkzeugID",
                table: "StandardWerkzeugZuweisungen",
                column: "WerkzeugID");

            migrationBuilder.CreateIndex(
                name: "IX_Werkzeuge_WerkzeugUnterkategorieID",
                table: "Werkzeuge",
                column: "WerkzeugUnterkategorieID");

            migrationBuilder.CreateIndex(
                name: "IX_WerkzeugEinsaetze_NCProgrammID",
                table: "WerkzeugEinsaetze",
                column: "NCProgrammID");

            migrationBuilder.CreateIndex(
                name: "IX_WerkzeugEinsaetze_WerkzeugID",
                table: "WerkzeugEinsaetze",
                column: "WerkzeugID");

            migrationBuilder.CreateIndex(
                name: "IX_WerkzeugUnterkategorien_WerkzeugKategorieID",
                table: "WerkzeugUnterkategorien",
                column: "WerkzeugKategorieID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StandardWerkzeugZuweisungen");

            migrationBuilder.DropTable(
                name: "WerkzeugEinsaetze");

            migrationBuilder.DropTable(
                name: "NCProgramme");

            migrationBuilder.DropTable(
                name: "Werkzeuge");

            migrationBuilder.DropTable(
                name: "Projekte");

            migrationBuilder.DropTable(
                name: "WerkzeugUnterkategorien");

            migrationBuilder.DropTable(
                name: "Maschinen");

            migrationBuilder.DropTable(
                name: "WerkzeugKategorien");

            migrationBuilder.DropTable(
                name: "Hersteller");

            migrationBuilder.DropTable(
                name: "Standorte");
        }
    }
}
