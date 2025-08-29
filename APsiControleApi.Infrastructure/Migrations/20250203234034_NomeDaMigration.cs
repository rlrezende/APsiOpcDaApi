using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APsiControleApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NomeDaMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "APsiCDb");

            migrationBuilder.CreateTable(
                name: "Controle",
                schema: "APsiCDb",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UnidadeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Controle", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                schema: "APsiCDb",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UnidadeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tag", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leitura",
                schema: "APsiCDb",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataLeitura = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Valor = table.Column<double>(type: "double precision", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leitura", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leitura_Tag_TagId",
                        column: x => x.TagId,
                        principalSchema: "APsiCDb",
                        principalTable: "Tag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Controle_UnidadeId",
                schema: "APsiCDb",
                table: "Controle",
                column: "UnidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_Leitura_TagId",
                schema: "APsiCDb",
                table: "Leitura",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_UnidadeId",
                schema: "APsiCDb",
                table: "Tag",
                column: "UnidadeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Controle",
                schema: "APsiCDb");

            migrationBuilder.DropTable(
                name: "Leitura",
                schema: "APsiCDb");

            migrationBuilder.DropTable(
                name: "Tag",
                schema: "APsiCDb");
        }
    }
}
