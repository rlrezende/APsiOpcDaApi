using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APsiControleApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDetectModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DetectModel",
                schema: "APsiCDb",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    InstrumentClass = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScheduleMinutes = table.Column<int>(type: "integer", nullable: false),
                    TargetAccuracy = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeployedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DriftPercent = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectModel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DetectModelPipeline",
                schema: "APsiCDb",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectModelPipeline", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetectModelPipeline_DetectModel_DetectModelId",
                        column: x => x.DetectModelId,
                        principalSchema: "APsiCDb",
                        principalTable: "DetectModel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetectModelTag",
                schema: "APsiCDb",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SeverityBaseline = table.Column<double>(type: "double precision", nullable: false),
                    ExpectedStdDev = table.Column<double>(type: "double precision", nullable: true),
                    PvMvRelation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "none"),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectModelTag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetectModelTag_DetectModel_DetectModelId",
                        column: x => x.DetectModelId,
                        principalSchema: "APsiCDb",
                        principalTable: "DetectModel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetectTrainingJob",
                schema: "APsiCDb",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectModelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectTrainingJob", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetectTrainingJob_DetectModel_DetectModelId",
                        column: x => x.DetectModelId,
                        principalSchema: "APsiCDb",
                        principalTable: "DetectModel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetectModelPipeline_DetectModelId",
                schema: "APsiCDb",
                table: "DetectModelPipeline",
                column: "DetectModelId");

            migrationBuilder.CreateIndex(
                name: "IX_DetectModelTag_DetectModelId",
                schema: "APsiCDb",
                table: "DetectModelTag",
                column: "DetectModelId");

            migrationBuilder.CreateIndex(
                name: "IX_DetectTrainingJob_DetectModelId",
                schema: "APsiCDb",
                table: "DetectTrainingJob",
                column: "DetectModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetectModelPipeline",
                schema: "APsiCDb");

            migrationBuilder.DropTable(
                name: "DetectModelTag",
                schema: "APsiCDb");

            migrationBuilder.DropTable(
                name: "DetectTrainingJob",
                schema: "APsiCDb");

            migrationBuilder.DropTable(
                name: "DetectModel",
                schema: "APsiCDb");
        }
    }
}
