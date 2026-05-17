using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TailorCV.CVGenerator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cvgenerator");

            migrationBuilder.CreateTable(
                name: "generated_cvs",
                schema: "cvgenerator",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    job_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: true),
                    match_score = table.Column<string>(type: "jsonb", nullable: true),
                    cover_letter = table.Column<string>(type: "text", nullable: true),
                    generation_type = table.Column<int>(type: "integer", nullable: false),
                    tailoring_prompt = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    pdf_key = table.Column<string>(type: "text", nullable: true),
                    pdf_status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_generated_cvs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_generated_cvs_created_at",
                schema: "cvgenerator",
                table: "generated_cvs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_generated_cvs_status",
                schema: "cvgenerator",
                table: "generated_cvs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_generated_cvs_user_id",
                schema: "cvgenerator",
                table: "generated_cvs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "generated_cvs",
                schema: "cvgenerator");
        }
    }
}
