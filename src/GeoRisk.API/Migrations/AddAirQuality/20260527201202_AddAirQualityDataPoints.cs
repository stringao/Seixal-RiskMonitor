using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable
#pragma warning disable S1192 // String literals should not be duplicated

namespace GeoRisk.API.Migrations.AddAirQuality
{
    /// <inheritdoc />
    public partial class AddAirQualityDataPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AirQualityDataPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<Point>(type: "geometry", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Pm10 = table.Column<double>(type: "double precision", nullable: true),
                    Pm25 = table.Column<double>(type: "double precision", nullable: true),
                    NitrogenDioxide = table.Column<double>(type: "double precision", nullable: true),
                    Ozone = table.Column<double>(type: "double precision", nullable: true),
                    SulphurDioxide = table.Column<double>(type: "double precision", nullable: true),
                    CarbonMonoxide = table.Column<double>(type: "double precision", nullable: true),
                    Dust = table.Column<double>(type: "double precision", nullable: true),
                    AerosolOpticalDepth = table.Column<double>(type: "double precision", nullable: true),
                    AqiValue = table.Column<int>(type: "integer", nullable: true),
                    AqiCategory = table.Column<string>(type: "text", nullable: true),
                    DominantPollutant = table.Column<string>(type: "text", nullable: true),
                    GrassPollen = table.Column<double>(type: "double precision", nullable: true),
                    OlivePollen = table.Column<double>(type: "double precision", nullable: true),
                    AlderPollen = table.Column<double>(type: "double precision", nullable: true),
                    BirchPollen = table.Column<double>(type: "double precision", nullable: true),
                    MugwortPollen = table.Column<double>(type: "double precision", nullable: true),
                    RagweedPollen = table.Column<double>(type: "double precision", nullable: true),
                    PollenIndex = table.Column<int>(type: "integer", nullable: true),
                    PollenCategory = table.Column<string>(type: "text", nullable: true),
                    DominantPollen = table.Column<string>(type: "text", nullable: true),
                    HealthRiskLevel = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GridPointId = table.Column<string>(type: "text", nullable: true),
                    Municipality = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AirQualityDataPoints", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AirQualityDataPoints");
        }
    }
}
