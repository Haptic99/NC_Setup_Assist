using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NC_Setup_Assist.Migrations
{
    /// <inheritdoc />
    public partial class AddVerwendetKorbToWerkzeugEinsatz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "VerwendetKorb",
                table: "WerkzeugEinsaetze",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerwendetKorb",
                table: "WerkzeugEinsaetze");
        }
    }
}
