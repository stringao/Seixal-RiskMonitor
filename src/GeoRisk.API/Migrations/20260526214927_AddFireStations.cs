using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable
#pragma warning disable S1192 // String literals should not be duplicated

namespace GeoRisk.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFireStations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FireStations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    County = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Parish = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Geometry = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OperationalZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Cim = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PersonnelCount = table.Column<int>(type: "integer", nullable: false),
                    VehicleCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FireStations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FireStations_Code",
                table: "FireStations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FireStations_County",
                table: "FireStations",
                column: "County");

            migrationBuilder.CreateIndex(
                name: "IX_FireStations_District",
                table: "FireStations",
                column: "District");

            migrationBuilder.CreateIndex(
                name: "IX_FireStations_Geometry",
                table: "FireStations",
                column: "Geometry")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_FireStations_Type",
                table: "FireStations",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FireStations");
        }
    }
}
