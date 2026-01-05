using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APsiOpcDaApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeIdOpcToTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NodeIdOpc",
                schema: "APsiCDb",
                table: "Tag",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NodeIdOpc",
                schema: "APsiCDb",
                table: "Tag");
        }
    }
}

