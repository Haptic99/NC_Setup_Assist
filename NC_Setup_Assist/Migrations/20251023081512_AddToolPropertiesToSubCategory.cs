using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NC_Setup_Assist.Migrations
{
    /// <inheritdoc />
    public partial class AddToolPropertiesToSubCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BenötigtPlattenwinkel",
                table: "WerkzeugUnterkategorien",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BenötigtSteigung",
                table: "WerkzeugUnterkategorien",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Plattenwinkel",
                table: "Werkzeuge",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BenötigtPlattenwinkel",
                table: "WerkzeugUnterkategorien");

            migrationBuilder.DropColumn(
                name: "BenötigtSteigung",
                table: "WerkzeugUnterkategorien");

            migrationBuilder.DropColumn(
                name: "Plattenwinkel",
                table: "Werkzeuge");
        }
    }
}
