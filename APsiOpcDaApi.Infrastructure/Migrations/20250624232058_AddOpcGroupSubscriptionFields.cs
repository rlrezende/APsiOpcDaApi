using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APsiOpcDaApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOpcGroupSubscriptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KeepAliveCount",
                schema: "APsiCDb",
                table: "OpcGroup",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "LifetimeCount",
                schema: "APsiCDb",
                table: "OpcGroup",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "MaxNotificationsPerPublish",
                schema: "APsiCDb",
                table: "OpcGroup",
                type: "integer",
                nullable: false,
                defaultValue: 1000);

            migrationBuilder.AddColumn<byte>(
                name: "Priority",
                schema: "APsiCDb",
                table: "OpcGroup",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KeepAliveCount",
                schema: "APsiCDb",
                table: "OpcGroup");

            migrationBuilder.DropColumn(
                name: "LifetimeCount",
                schema: "APsiCDb",
                table: "OpcGroup");

            migrationBuilder.DropColumn(
                name: "MaxNotificationsPerPublish",
                schema: "APsiCDb",
                table: "OpcGroup");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "APsiCDb",
                table: "OpcGroup");
        }
    }
}

