using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NC_Setup_Assist.Migrations
{
    /// <inheritdoc />
    public partial class AddRadiusToWerkzeug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BenötigtRadius",
                table: "WerkzeugUnterkategorien",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Radius",
                table: "Werkzeuge",
                type: "REAL",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "WerkzeugUnterkategorien",
                keyColumn: "WerkzeugUnterkategorieID",
                keyValue: 1,
                column: "BenötigtRadius",
                value: false);

            migrationBuilder.UpdateData(
                table: "WerkzeugUnterkategorien",
                keyColumn: "WerkzeugUnterkategorieID",
                keyValue: 2,
                column: "BenötigtRadius",
                value: false);

            migrationBuilder.UpdateData(
                table: "WerkzeugUnterkategorien",
                keyColumn: "WerkzeugUnterkategorieID",
                keyValue: 3,
                column: "BenötigtRadius",
                value: false);

            migrationBuilder.UpdateData(
                table: "WerkzeugUnterkategorien",
                keyColumn: "WerkzeugUnterkategorieID",
                keyValue: 4,
                column: "BenötigtRadius",
                value: false);

            migrationBuilder.InsertData(
                table: "WerkzeugUnterkategorien",
                columns: new[] { "WerkzeugUnterkategorieID", "BenötigtPlattenwinkel", "BenötigtRadius", "BenötigtSteigung", "Name", "WerkzeugKategorieID" },
                values: new object[] { 5, false, false, false, "Kugelfräser", 1 });

            migrationBuilder.UpdateData(
                table: "Werkzeuge",
                keyColumn: "WerkzeugID",
                keyValue: 1,
                column: "Radius",
                value: null);

            migrationBuilder.UpdateData(
                table: "Werkzeuge",
                keyColumn: "WerkzeugID",
                keyValue: 2,
                column: "Radius",
                value: null);

            migrationBuilder.UpdateData(
                table: "Werkzeuge",
                keyColumn: "WerkzeugID",
                keyValue: 3,
                column: "Radius",
                value: null);

            migrationBuilder.UpdateData(
                table: "Werkzeuge",
                keyColumn: "WerkzeugID",
                keyValue: 4,
                column: "Radius",
                value: null);

            migrationBuilder.InsertData(
                table: "Werkzeuge",
                columns: new[] { "WerkzeugID", "Beschreibung", "Name", "Plattenwinkel", "Radius", "Steigung", "WerkzeugUnterkategorieID" },
                values: new object[] { 5, null, "Gravurstichel Ø1, R0.5", null, null, null, 5 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Werkzeuge",
                keyColumn: "WerkzeugID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "WerkzeugUnterkategorien",
                keyColumn: "WerkzeugUnterkategorieID",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "BenötigtRadius",
                table: "WerkzeugUnterkategorien");

            migrationBuilder.DropColumn(
                name: "Radius",
                table: "Werkzeuge");
        }
    }
}
