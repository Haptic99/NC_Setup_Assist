using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NC_Setup_Assist.Migrations
{
    /// <inheritdoc />
    public partial class SeedStandardData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "WerkzeugKategorien",
                columns: new[] { "WerkzeugKategorieID", "Name" },
                values: new object[,]
                {
                    { 1, "Fräser" },
                    { 2, "Bohrer" },
                    { 3, "Drehwerkzeug" }
                });

            migrationBuilder.InsertData(
                table: "WerkzeugUnterkategorien",
                columns: new[] { "WerkzeugUnterkategorieID", "BenötigtPlattenwinkel", "BenötigtSteigung", "Name", "WerkzeugKategorieID" },
                values: new object[,]
                {
                    { 1, false, true, "Gewindedrehstahl Aussen", 3 },
                    { 2, false, true, "Gewindedrehstahl Innen", 3 },
                    { 3, true, false, "Messerst.", 3 },
                    { 4, false, false, "Abstechstähle", 3 }
                });

            migrationBuilder.InsertData(
                table: "Werkzeuge",
                columns: new[] { "WerkzeugID", "Beschreibung", "Name", "Plattenwinkel", "Steigung", "WerkzeugUnterkategorieID" },
                values: new object[,]
                {
                    { 1, null, "Gewindedrehstahl Aussen P=0.75", null, 0.75, 1 },
                    { 2, null, "Messerst. 80°", 80.0, null, 3 },
                    { 3, null, "Messerst. 35° gekr.", 35.0, null, 3 },
                    { 4, null, "Abstechst. B=3mm", null, null, 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "WerkzeugKategorien",
                keyColumn: "WerkzeugKategorieID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "WerkzeugKategorien",
                keyColumn: "WerkzeugKategorieID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "WerkzeugUnterkategorien",
                keyColumn: "WerkzeugUnterkategorieID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Werkzeuge",
                keyColumn: "WerkzeugID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Werkzeuge",
                keyColumn: "WerkzeugID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Werkzeuge",
                keyColumn: "WerkzeugID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Werkzeuge",
                keyColumn: "WerkzeugID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "WerkzeugUnterkategorien",
                keyColumn: "WerkzeugUnterkategorieID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "WerkzeugUnterkategorien",
                keyColumn: "WerkzeugUnterkategorieID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "WerkzeugUnterkategorien",
                keyColumn: "WerkzeugUnterkategorieID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "WerkzeugKategorien",
                keyColumn: "WerkzeugKategorieID",
                keyValue: 3);
        }
    }
}
