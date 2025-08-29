using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APsiControleApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOpcGroupsAndDiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                schema: "APsiCDb",
                table: "Tag",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId1",
                schema: "APsiCDb",
                table: "Tag",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OpcDiscoveredServer",
                schema: "APsiCDb",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ApplicationUri = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DiscoveryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    SecurityModes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NetworkRange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ResponseTime = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpcDiscoveredServer", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpcGroup",
                schema: "APsiCDb",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ServerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdateRate = table.Column<int>(type: "integer", nullable: false, defaultValue: 1000),
                    Deadband = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.10000000000000001),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpcGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpcGroup_OpcServer_ServerId",
                        column: x => x.ServerId,
                        principalSchema: "APsiCDb",
                        principalTable: "OpcServer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tag_GroupId",
                schema: "APsiCDb",
                table: "Tag",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_GroupId1",
                schema: "APsiCDb",
                table: "Tag",
                column: "GroupId1");

            migrationBuilder.CreateIndex(
                name: "IX_OpcDiscoveredServer_Endpoint",
                schema: "APsiCDb",
                table: "OpcDiscoveredServer",
                column: "Endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpcGroup_ServerId",
                schema: "APsiCDb",
                table: "OpcGroup",
                column: "ServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_OpcGroup_GroupId",
                schema: "APsiCDb",
                table: "Tag",
                column: "GroupId",
                principalSchema: "APsiCDb",
                principalTable: "OpcGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_OpcGroup_GroupId1",
                schema: "APsiCDb",
                table: "Tag",
                column: "GroupId1",
                principalSchema: "APsiCDb",
                principalTable: "OpcGroup",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tag_OpcGroup_GroupId",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropForeignKey(
                name: "FK_Tag_OpcGroup_GroupId1",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropTable(
                name: "OpcDiscoveredServer",
                schema: "APsiCDb");

            migrationBuilder.DropTable(
                name: "OpcGroup",
                schema: "APsiCDb");

            migrationBuilder.DropIndex(
                name: "IX_Tag_GroupId",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropIndex(
                name: "IX_Tag_GroupId1",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "GroupId",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "GroupId1",
                schema: "APsiCDb",
                table: "Tag");
        }
    }
}
