using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using TailorCV.Profile.Domain;

#nullable disable

namespace TailorCV.Profile.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class JsonBSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "certifications",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "custom_sections",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "education",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "experiences",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "languages",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "section_orders",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "skills",
                schema: "profile");

            migrationBuilder.AddColumn<List<ProfileSection>>(
                name: "sections",
                schema: "profile",
                table: "profiles",
                type: "jsonb",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sections",
                schema: "profile",
                table: "profiles");

            migrationBuilder.CreateTable(
                name: "certifications",
                schema: "profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    issuer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_certifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_certifications_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "profile",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "custom_sections",
                schema: "profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    items = table.Column<string>(type: "jsonb", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_sections", x => x.id);
                    table.ForeignKey(
                        name: "fk_custom_sections_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "profile",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "education",
                schema: "profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    degree = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    field = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    gpa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    institution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_education", x => x.id);
                    table.ForeignKey(
                        name: "fk_education_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "profile",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experiences",
                schema: "profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_experiences", x => x.id);
                    table.ForeignKey(
                        name: "fk_experiences_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "profile",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "languages",
                schema: "profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    proficiency = table.Column<int>(type: "integer", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_languages", x => x.id);
                    table.ForeignKey(
                        name: "fk_languages_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "profile",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    tech_stack = table.Column<string>(type: "jsonb", nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.ForeignKey(
                        name: "fk_projects_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "profile",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "section_orders",
                schema: "profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_section_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_section_orders_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "profile",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                schema: "profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    items = table.Column<string>(type: "jsonb", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skills", x => x.id);
                    table.ForeignKey(
                        name: "fk_skills_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "profile",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_certifications_profile_id",
                schema: "profile",
                table: "certifications",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_custom_sections_profile_id",
                schema: "profile",
                table: "custom_sections",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_education_profile_id",
                schema: "profile",
                table: "education",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_experiences_profile_id",
                schema: "profile",
                table: "experiences",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_languages_profile_id",
                schema: "profile",
                table: "languages",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_profile_id",
                schema: "profile",
                table: "projects",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_section_orders_profile_id_section_id",
                schema: "profile",
                table: "section_orders",
                columns: new[] { "profile_id", "section_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_skills_profile_id",
                schema: "profile",
                table: "skills",
                column: "profile_id");
        }
    }
}
