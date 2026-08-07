using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APsiOpcDaApi.Infrastructure.Migrations
{
    [DbContext(typeof(APsiOpcDaApiContext))]
    [Migration("20260805120000_RemoveDuplicateTagGroupId")]
    public partial class RemoveDuplicateTagGroupId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserva vínculos gravados pelo relacionamento duplicado antes de removê-lo.
            migrationBuilder.Sql(
                """
                UPDATE "APsiCDb"."Tag"
                SET "GroupId" = "GroupId1"
                WHERE "GroupId" IS NULL
                  AND "GroupId1" IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Tag_OpcGroup_GroupId1",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropIndex(
                name: "IX_Tag_GroupId1",
                schema: "APsiCDb",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "GroupId1",
                schema: "APsiCDb",
                table: "Tag");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId1",
                schema: "APsiCDb",
                table: "Tag",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tag_GroupId1",
                schema: "APsiCDb",
                table: "Tag",
                column: "GroupId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_OpcGroup_GroupId1",
                schema: "APsiCDb",
                table: "Tag",
                column: "GroupId1",
                principalSchema: "APsiCDb",
                principalTable: "OpcGroup",
                principalColumn: "Id");
        }
    }
}
