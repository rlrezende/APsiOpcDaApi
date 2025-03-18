using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APsiControleApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdOldToTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "idOld",
                schema: "APsiCDb",
                table: "Tag",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "idOld",
                schema: "APsiCDb",
                table: "Tag");
        }
    }
}
