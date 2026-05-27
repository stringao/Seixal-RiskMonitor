using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace GeoRisk.API.Migrations.AddAllRemaining
{
    /// <inheritdoc />
    public partial class AddNewEntitiesForPreviouslyExcludedFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FireResources_FireStations_FireStationId",
                table: "FireResources");

            migrationBuilder.DropForeignKey(
                name: "FK_FireResources_GeoEvents_DeployedToEventId",
                table: "FireResources");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceDispatches_FireResources_ResourceId",
                table: "ResourceDispatches");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceDispatches_GeoEvents_EventId",
                table: "ResourceDispatches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ResourceDispatches",
                table: "ResourceDispatches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FireResources",
                table: "FireResources");

            migrationBuilder.RenameTable(
                name: "ResourceDispatches",
                newName: "resource_dispatches");

            migrationBuilder.RenameTable(
                name: "FireResources",
                newName: "fire_resources");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "resource_dispatches",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "resource_dispatches",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TravelTimeMinutes",
                table: "resource_dispatches",
                newName: "travel_time_minutes");

            migrationBuilder.RenameColumn(
                name: "RoutePolyline",
                table: "resource_dispatches",
                newName: "route_polyline");

            migrationBuilder.RenameColumn(
                name: "ResourceId",
                table: "resource_dispatches",
                newName: "resource_id");

            migrationBuilder.RenameColumn(
                name: "EventId",
                table: "resource_dispatches",
                newName: "event_id");

            migrationBuilder.RenameColumn(
                name: "DistanceKm",
                table: "resource_dispatches",
                newName: "distance_km");

            migrationBuilder.RenameColumn(
                name: "DispatchedAt",
                table: "resource_dispatches",
                newName: "dispatched_at");

            migrationBuilder.RenameColumn(
                name: "ArrivedAt",
                table: "resource_dispatches",
                newName: "arrived_at");

            migrationBuilder.RenameIndex(
                name: "IX_ResourceDispatches_ResourceId",
                table: "resource_dispatches",
                newName: "IX_resource_dispatches_resource_id");

            migrationBuilder.RenameIndex(
                name: "IX_ResourceDispatches_EventId",
                table: "resource_dispatches",
                newName: "IX_resource_dispatches_event_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "fire_resources",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "fire_resources",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "fire_resources",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WaterCapacityLiters",
                table: "fire_resources",
                newName: "water_capacity_liters");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "fire_resources",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "ResourceType",
                table: "fire_resources",
                newName: "resource_type");

            migrationBuilder.RenameColumn(
                name: "PersonnelCount",
                table: "fire_resources",
                newName: "personnel_count");

            migrationBuilder.RenameColumn(
                name: "IsAvailable",
                table: "fire_resources",
                newName: "is_available");

            migrationBuilder.RenameColumn(
                name: "FireStationId",
                table: "fire_resources",
                newName: "fire_station_id");

            migrationBuilder.RenameColumn(
                name: "ExpectedReturnAt",
                table: "fire_resources",
                newName: "expected_return_at");

            migrationBuilder.RenameColumn(
                name: "DeployedToEventId",
                table: "fire_resources",
                newName: "deployed_to_event_id");

            migrationBuilder.RenameColumn(
                name: "DeployedAt",
                table: "fire_resources",
                newName: "deployed_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "fire_resources",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_FireResources_FireStationId",
                table: "fire_resources",
                newName: "IX_fire_resources_fire_station_id");

            migrationBuilder.RenameIndex(
                name: "IX_FireResources_DeployedToEventId",
                table: "fire_resources",
                newName: "IX_fire_resources_deployed_to_event_id");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "resource_dispatches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Dispatched",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "route_polyline",
                table: "resource_dispatches",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "fire_resources",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Available",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "is_available",
                table: "fire_resources",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddPrimaryKey(
                name: "PK_resource_dispatches",
                table: "resource_dispatches",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_fire_resources",
                table: "fire_resources",
                column: "id");

            migrationBuilder.CreateTable(
                name: "event_chain_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    primary_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    confidence_score = table.Column<double>(type: "double precision", nullable: false),
                    findings = table.Column<string>(type: "jsonb", nullable: true),
                    contributing_factors = table.Column<List<string>>(type: "text[]", nullable: false),
                    recommended_action = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    analyzed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_chain_analyses", x => x.id);
                    table.ForeignKey(
                        name: "FK_event_chain_analyses_GeoEvents_primary_event_id",
                        column: x => x.primary_event_id,
                        principalTable: "GeoEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "land_use_data_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    location = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cos_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cos_description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fuel_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fuel_load = table.Column<int>(type: "integer", nullable: false),
                    fire_risk_multiplier = table.Column<double>(type: "double precision", nullable: false),
                    dominant_species = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_wildland_urban_interface = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    grid_point_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_land_use_data_points", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_resource_dispatches_dispatched_at",
                table: "resource_dispatches",
                column: "dispatched_at");

            migrationBuilder.CreateIndex(
                name: "IX_resource_dispatches_status",
                table: "resource_dispatches",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_fire_resources_is_available",
                table: "fire_resources",
                column: "is_available");

            migrationBuilder.CreateIndex(
                name: "IX_fire_resources_status",
                table: "fire_resources",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_event_chain_analyses_analysis_type",
                table: "event_chain_analyses",
                column: "analysis_type");

            migrationBuilder.CreateIndex(
                name: "IX_event_chain_analyses_analyzed_at",
                table: "event_chain_analyses",
                column: "analyzed_at");

            migrationBuilder.CreateIndex(
                name: "IX_event_chain_analyses_confidence_score",
                table: "event_chain_analyses",
                column: "confidence_score");

            migrationBuilder.CreateIndex(
                name: "IX_event_chain_analyses_primary_event_id",
                table: "event_chain_analyses",
                column: "primary_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_land_use_data_points_cos_code",
                table: "land_use_data_points",
                column: "cos_code");

            migrationBuilder.CreateIndex(
                name: "IX_land_use_data_points_grid_point_id",
                table: "land_use_data_points",
                column: "grid_point_id");

            migrationBuilder.CreateIndex(
                name: "IX_land_use_data_points_location",
                table: "land_use_data_points",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_land_use_data_points_timestamp",
                table: "land_use_data_points",
                column: "timestamp");

            migrationBuilder.AddForeignKey(
                name: "FK_fire_resources_FireStations_fire_station_id",
                table: "fire_resources",
                column: "fire_station_id",
                principalTable: "FireStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_fire_resources_GeoEvents_deployed_to_event_id",
                table: "fire_resources",
                column: "deployed_to_event_id",
                principalTable: "GeoEvents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_resource_dispatches_GeoEvents_event_id",
                table: "resource_dispatches",
                column: "event_id",
                principalTable: "GeoEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_resource_dispatches_fire_resources_resource_id",
                table: "resource_dispatches",
                column: "resource_id",
                principalTable: "fire_resources",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fire_resources_FireStations_fire_station_id",
                table: "fire_resources");

            migrationBuilder.DropForeignKey(
                name: "FK_fire_resources_GeoEvents_deployed_to_event_id",
                table: "fire_resources");

            migrationBuilder.DropForeignKey(
                name: "FK_resource_dispatches_GeoEvents_event_id",
                table: "resource_dispatches");

            migrationBuilder.DropForeignKey(
                name: "FK_resource_dispatches_fire_resources_resource_id",
                table: "resource_dispatches");

            migrationBuilder.DropTable(
                name: "event_chain_analyses");

            migrationBuilder.DropTable(
                name: "land_use_data_points");

            migrationBuilder.DropPrimaryKey(
                name: "PK_resource_dispatches",
                table: "resource_dispatches");

            migrationBuilder.DropIndex(
                name: "IX_resource_dispatches_dispatched_at",
                table: "resource_dispatches");

            migrationBuilder.DropIndex(
                name: "IX_resource_dispatches_status",
                table: "resource_dispatches");

            migrationBuilder.DropPrimaryKey(
                name: "PK_fire_resources",
                table: "fire_resources");

            migrationBuilder.DropIndex(
                name: "IX_fire_resources_is_available",
                table: "fire_resources");

            migrationBuilder.DropIndex(
                name: "IX_fire_resources_status",
                table: "fire_resources");

            migrationBuilder.RenameTable(
                name: "resource_dispatches",
                newName: "ResourceDispatches");

            migrationBuilder.RenameTable(
                name: "fire_resources",
                newName: "FireResources");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "ResourceDispatches",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ResourceDispatches",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "travel_time_minutes",
                table: "ResourceDispatches",
                newName: "TravelTimeMinutes");

            migrationBuilder.RenameColumn(
                name: "route_polyline",
                table: "ResourceDispatches",
                newName: "RoutePolyline");

            migrationBuilder.RenameColumn(
                name: "resource_id",
                table: "ResourceDispatches",
                newName: "ResourceId");

            migrationBuilder.RenameColumn(
                name: "event_id",
                table: "ResourceDispatches",
                newName: "EventId");

            migrationBuilder.RenameColumn(
                name: "distance_km",
                table: "ResourceDispatches",
                newName: "DistanceKm");

            migrationBuilder.RenameColumn(
                name: "dispatched_at",
                table: "ResourceDispatches",
                newName: "DispatchedAt");

            migrationBuilder.RenameColumn(
                name: "arrived_at",
                table: "ResourceDispatches",
                newName: "ArrivedAt");

            migrationBuilder.RenameIndex(
                name: "IX_resource_dispatches_resource_id",
                table: "ResourceDispatches",
                newName: "IX_ResourceDispatches_ResourceId");

            migrationBuilder.RenameIndex(
                name: "IX_resource_dispatches_event_id",
                table: "ResourceDispatches",
                newName: "IX_ResourceDispatches_EventId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "FireResources",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "FireResources",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "FireResources",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "water_capacity_liters",
                table: "FireResources",
                newName: "WaterCapacityLiters");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "FireResources",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "resource_type",
                table: "FireResources",
                newName: "ResourceType");

            migrationBuilder.RenameColumn(
                name: "personnel_count",
                table: "FireResources",
                newName: "PersonnelCount");

            migrationBuilder.RenameColumn(
                name: "is_available",
                table: "FireResources",
                newName: "IsAvailable");

            migrationBuilder.RenameColumn(
                name: "fire_station_id",
                table: "FireResources",
                newName: "FireStationId");

            migrationBuilder.RenameColumn(
                name: "expected_return_at",
                table: "FireResources",
                newName: "ExpectedReturnAt");

            migrationBuilder.RenameColumn(
                name: "deployed_to_event_id",
                table: "FireResources",
                newName: "DeployedToEventId");

            migrationBuilder.RenameColumn(
                name: "deployed_at",
                table: "FireResources",
                newName: "DeployedAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "FireResources",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_fire_resources_fire_station_id",
                table: "FireResources",
                newName: "IX_FireResources_FireStationId");

            migrationBuilder.RenameIndex(
                name: "IX_fire_resources_deployed_to_event_id",
                table: "FireResources",
                newName: "IX_FireResources_DeployedToEventId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ResourceDispatches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Dispatched");

            migrationBuilder.AlterColumn<string>(
                name: "RoutePolyline",
                table: "ResourceDispatches",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "FireResources",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Available");

            migrationBuilder.AlterColumn<bool>(
                name: "IsAvailable",
                table: "FireResources",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResourceDispatches",
                table: "ResourceDispatches",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FireResources",
                table: "FireResources",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FireResources_FireStations_FireStationId",
                table: "FireResources",
                column: "FireStationId",
                principalTable: "FireStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FireResources_GeoEvents_DeployedToEventId",
                table: "FireResources",
                column: "DeployedToEventId",
                principalTable: "GeoEvents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceDispatches_FireResources_ResourceId",
                table: "ResourceDispatches",
                column: "ResourceId",
                principalTable: "FireResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceDispatches_GeoEvents_EventId",
                table: "ResourceDispatches",
                column: "EventId",
                principalTable: "GeoEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
