using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TailorCV.JobDescriptions.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jobdescriptions");

            migrationBuilder.CreateTable(
                name: "job_descriptions",
                schema: "jobdescriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    required_skills = table.Column<List<string>>(type: "text[]", nullable: false),
                    responsibilities = table.Column<List<string>>(type: "text[]", nullable: false),
                    qualifications = table.Column<List<string>>(type: "text[]", nullable: false),
                    seniority_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    source_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    raw_text = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_descriptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parse_jobs",
                schema: "jobdescriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    raw_text = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    parsed_data = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parse_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_job_descriptions_user_id",
                schema: "jobdescriptions",
                table: "job_descriptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_parse_jobs_status",
                schema: "jobdescriptions",
                table: "parse_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_parse_jobs_user_id",
                schema: "jobdescriptions",
                table: "parse_jobs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_descriptions",
                schema: "jobdescriptions");

            migrationBuilder.DropTable(
                name: "parse_jobs",
                schema: "jobdescriptions");
        }
    }
}
