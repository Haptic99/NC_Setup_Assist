using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NC_Setup_Assist.Migrations
{
    /// <inheritdoc />
    public partial class RenameBearbeitungsartAndAddFavoritenToWerkzeugEinsatz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BearbeitungsArt",
                table: "WerkzeugEinsaetze",
                newName: "FräserAusrichtung");

            migrationBuilder.AddColumn<string>(
                name: "FavoritKategorie",
                table: "WerkzeugEinsaetze",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FavoritUnterkategorie",
                table: "WerkzeugEinsaetze",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FavoritKategorie",
                table: "WerkzeugEinsaetze");

            migrationBuilder.DropColumn(
                name: "FavoritUnterkategorie",
                table: "WerkzeugEinsaetze");

            migrationBuilder.RenameColumn(
                name: "FräserAusrichtung",
                table: "WerkzeugEinsaetze",
                newName: "BearbeitungsArt");
        }
    }
}
