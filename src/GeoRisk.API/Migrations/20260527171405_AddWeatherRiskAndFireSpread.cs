using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable
#pragma warning disable S1192 // String literals should not be duplicated

namespace GeoRisk.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWeatherRiskAndFireSpread : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FireSpreadPredictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FireEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    HorizonHours = table.Column<int>(type: "integer", nullable: false),
                    Polygon = table.Column<Polygon>(type: "geometry(Polygon, 4326)", nullable: false),
                    RosKmh = table.Column<double>(type: "double precision", nullable: false),
                    AreaKm2 = table.Column<double>(type: "double precision", nullable: false),
                    AffectedMunicipalities = table.Column<string>(type: "jsonb", nullable: false),
                    Scenario = table.Column<int>(type: "integer", nullable: false),
                    Conclusion = table.Column<string>(type: "text", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FWI = table.Column<double>(type: "double precision", nullable: false),
                    ISI = table.Column<double>(type: "double precision", nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: false),
                    Humidity = table.Column<double>(type: "double precision", nullable: false),
                    WindSpeed = table.Column<double>(type: "double precision", nullable: false),
                    WindDirection = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FireSpreadPredictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FireSpreadPredictions_GeoEvents_FireEventId",
                        column: x => x.FireEventId,
                        principalTable: "GeoEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeatherRiskDataPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: false),
                    Humidity = table.Column<double>(type: "double precision", nullable: false),
                    WindSpeed = table.Column<double>(type: "double precision", nullable: false),
                    WindDirection = table.Column<double>(type: "double precision", nullable: false),
                    Precipitation = table.Column<double>(type: "double precision", nullable: false),
                    FFMC = table.Column<double>(type: "double precision", nullable: false),
                    DMC = table.Column<double>(type: "double precision", nullable: false),
                    DC = table.Column<double>(type: "double precision", nullable: false),
                    ISI = table.Column<double>(type: "double precision", nullable: false),
                    BUI = table.Column<double>(type: "double precision", nullable: false),
                    FWI = table.Column<double>(type: "double precision", nullable: false),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    Conclusion = table.Column<string>(type: "text", nullable: true),
                    Municipality = table.Column<string>(type: "text", nullable: true),
                    GridPointId = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherRiskDataPoints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FireSpreadPredictions_FireEventId",
                table: "FireSpreadPredictions",
                column: "FireEventId");

            migrationBuilder.CreateIndex(
                name: "IX_FireSpreadPredictions_FireEventId_HorizonHours",
                table: "FireSpreadPredictions",
                columns: new[] { "FireEventId", "HorizonHours" });

            migrationBuilder.CreateIndex(
                name: "IX_FireSpreadPredictions_Polygon",
                table: "FireSpreadPredictions",
                column: "Polygon")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherRiskDataPoints_GridPointId",
                table: "WeatherRiskDataPoints",
                column: "GridPointId");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherRiskDataPoints_Location",
                table: "WeatherRiskDataPoints",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherRiskDataPoints_Timestamp",
                table: "WeatherRiskDataPoints",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FireSpreadPredictions");

            migrationBuilder.DropTable(
                name: "WeatherRiskDataPoints");
        }
    }
}
