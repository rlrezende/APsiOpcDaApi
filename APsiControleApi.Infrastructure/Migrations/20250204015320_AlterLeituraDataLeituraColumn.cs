using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APsiControleApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlterLeituraDataLeituraColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DataLeitura",
                schema: "APsiCDb",
                table: "Leitura",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DataLeitura",
                schema: "APsiCDb",
                table: "Leitura",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");
        }
    }
}
