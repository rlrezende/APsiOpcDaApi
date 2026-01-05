using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APsiOpcDaApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialOpcMonitoringSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Monitora",
                schema: "APsiCDb",
                table: "Tag",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "NodeId",
                schema: "APsiCDb",
                table: "Tag",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ValorAtual",
                schema: "APsiCDb",
                table: "Tag",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Erro",
                schema: "APsiCDb",
                table: "Leitura",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValorBruto",
                schema: "APsiCDb",
                table: "Leitura",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OpcServer",
                schema: "APsiCDb",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UnidadeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpcServer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpcNode",
                schema: "APsiCDb",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NodeId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpcNode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpcNode_OpcServer_ServerId",
                        column: x => x.ServerId,
                        principalSchema: "APsiCDb",
                        principalTable: "OpcServer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tag_NodeId",
                schema: "APsiCDb",
                table: "Tag",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_OpcNode_ServerId",
                schema: "APsiCDb",
                table: "OpcNode",
                column: "ServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_OpcNode_NodeId",
                schema: "APsiCDb",
                table: "Tag",
                column: "NodeId",
                principalSchema: "APsiCDb",
                principalTable: "OpcNode",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tag_OpcNode_NodeId",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropTable(
                name: "OpcNode",
                schema: "APsiCDb");

            migrationBuilder.DropTable(
                name: "OpcServer",
                schema: "APsiCDb");

            migrationBuilder.DropIndex(
                name: "IX_Tag_NodeId",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "Monitora",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "NodeId",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "ValorAtual",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "Erro",
                schema: "APsiCDb",
                table: "Leitura");

            migrationBuilder.DropColumn(
                name: "ValorBruto",
                schema: "APsiCDb",
                table: "Leitura");
        }
    }
}

