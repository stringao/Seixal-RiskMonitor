using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable
#pragma warning disable S1192 // String literals should not be duplicated

namespace GeoRisk.API.Migrations.AddAllRemaining
{
    /// <inheritdoc />
    public partial class AddAllRemainingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FireResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FireStationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PersonnelCount = table.Column<int>(type: "integer", nullable: false),
                    WaterCapacityLiters = table.Column<double>(type: "double precision", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    DeployedToEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeployedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpectedReturnAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FireResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FireResources_FireStations_FireStationId",
                        column: x => x.FireStationId,
                        principalTable: "FireStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FireResources_GeoEvents_DeployedToEventId",
                        column: x => x.DeployedToEventId,
                        principalTable: "GeoEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "satellite_fire_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    capture_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    full_resolution_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    footprint = table.Column<Polygon>(type: "geometry(Polygon, 4326)", nullable: true),
                    fire_radiative_power = table.Column<double>(type: "double precision", nullable: true),
                    brightness_temperature = table.Column<double>(type: "double precision", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    detection_confidence = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_satellite_fire_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_satellite_fire_images_GeoEvents_linked_event_id",
                        column: x => x.linked_event_id,
                        principalTable: "GeoEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceDispatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArrivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DistanceKm = table.Column<double>(type: "double precision", nullable: true),
                    TravelTimeMinutes = table.Column<double>(type: "double precision", nullable: true),
                    RoutePolyline = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceDispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceDispatches_FireResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "FireResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceDispatches_GeoEvents_EventId",
                        column: x => x.EventId,
                        principalTable: "GeoEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FireResources_DeployedToEventId",
                table: "FireResources",
                column: "DeployedToEventId");

            migrationBuilder.CreateIndex(
                name: "IX_FireResources_FireStationId",
                table: "FireResources",
                column: "FireStationId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceDispatches_EventId",
                table: "ResourceDispatches",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceDispatches_ResourceId",
                table: "ResourceDispatches",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_satellite_fire_images_capture_time",
                table: "satellite_fire_images",
                column: "capture_time");

            migrationBuilder.CreateIndex(
                name: "IX_satellite_fire_images_linked_event_id",
                table: "satellite_fire_images",
                column: "linked_event_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceDispatches");

            migrationBuilder.DropTable(
                name: "satellite_fire_images");

            migrationBuilder.DropTable(
                name: "FireResources");
        }
    }
}
