using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APsiControleApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposDatabaseOpcServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NomeColuna",
                schema: "APsiCDb",
                table: "Tag",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeTabela",
                schema: "APsiCDb",
                table: "Tag",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origem",
                schema: "APsiCDb",
                table: "Tag",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "OPCUA");

            migrationBuilder.AddColumn<string>(
                name: "ConnectionString",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomeColuna",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "NomeTabela",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "Origem",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "ConnectionString",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "Provider",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "Tipo",
                schema: "APsiCDb",
                table: "OpcServer");
        }
    }
}
