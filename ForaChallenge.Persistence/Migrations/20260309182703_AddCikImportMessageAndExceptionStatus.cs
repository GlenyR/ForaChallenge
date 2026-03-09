using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForaChallenge.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCikImportMessageAndExceptionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "CikImports",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Message",
                table: "CikImports");
        }
    }
}
