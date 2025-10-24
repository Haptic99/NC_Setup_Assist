using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NC_Setup_Assist.Migrations
{
    /// <inheritdoc />
    public partial class AddBearbeitungsArtToWerkzeugEinsatz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BearbeitungsArt",
                table: "WerkzeugEinsaetze",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BearbeitungsArt",
                table: "WerkzeugEinsaetze");
        }
    }
}
