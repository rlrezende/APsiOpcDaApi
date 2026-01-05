using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APsiOpcDaApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOpcServerMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClsId",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConnectionStatus",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscoveryTime",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Host",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConnected",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastConnection",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Password",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgId",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponseTime",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SecurityMode",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityPolicy",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClsId",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "ConnectionStatus",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "Descricao",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "DiscoveryTime",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "Host",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "IsConnected",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "LastConnection",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "Password",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "ProgId",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "ResponseTime",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "SecurityMode",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "SecurityPolicy",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.DropColumn(
                name: "Username",
                schema: "APsiCDb",
                table: "OpcServer");

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                schema: "APsiCDb",
                table: "OpcServer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}

