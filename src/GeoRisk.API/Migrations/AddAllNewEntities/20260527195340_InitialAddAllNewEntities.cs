using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable
#pragma warning disable S1192 // String literals should not be duplicated

namespace GeoRisk.API.Migrations.AddAllNewEntities
{
    /// <inheritdoc />
    public partial class InitialAddAllNewEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AlertRuleId",
                table: "Alerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AreaKm2",
                table: "Alerts",
                type: "numeric(10,4)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EscalatedFromAlertId",
                table: "Alerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FwiValue",
                table: "Alerts",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEscalated",
                table: "Alerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Temperature",
                table: "Alerts",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WindSpeed",
                table: "Alerts",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AreaKm2Threshold",
                table: "AlertRules",
                type: "numeric(10,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveCount",
                table: "AlertRules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EscalationMinutes",
                table: "AlertRules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxFwi",
                table: "AlertRules",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinFwi",
                table: "AlertRules",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinTemperature",
                table: "AlertRules",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MinWindSpeed",
                table: "AlertRules",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "NotifyRoles",
                table: "AlertRules",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeasonEndMonth",
                table: "AlertRules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeasonStartMonth",
                table: "AlertRules",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiContextCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContextType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    VectorHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiContextCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiContextCaches_GeoEvents_EventId",
                        column: x => x.EventId,
                        principalTable: "GeoEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CitizenAlertSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false),
                    RadiusKm = table.Column<double>(type: "double precision", nullable: false),
                    EventTypes = table.Column<string[]>(type: "text[]", nullable: false),
                    SeverityThreshold = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationToken = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenAlertSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DashboardCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CacheKey = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    GeneratedBy = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FireHotspots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false),
                    GridCellId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FireCount = table.Column<int>(type: "integer", nullable: false),
                    TotalAreaBurned = table.Column<double>(type: "double precision", nullable: false),
                    AverageSeverity = table.Column<int>(type: "integer", nullable: false),
                    PeakMonth = table.Column<int>(type: "integer", nullable: false),
                    PeakHour = table.Column<int>(type: "integer", nullable: false),
                    CommonWindDirection = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    AverageFwi = table.Column<double>(type: "double precision", nullable: false),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    CellGeometry = table.Column<Polygon>(type: "geometry(Polygon, 4326)", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FireHotspots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FireReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GeoEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GeneratedBy = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    ProbableCause = table.Column<string>(type: "text", nullable: true),
                    CauseConfidence = table.Column<double>(type: "double precision", nullable: true),
                    PeakFireTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PeakFireAreaHa = table.Column<double>(type: "double precision", nullable: true),
                    TotalAreaHa = table.Column<double>(type: "double precision", nullable: false),
                    AffectedAreas = table.Column<List<string>>(type: "text[]", nullable: false),
                    EvacuationCount = table.Column<int>(type: "integer", nullable: true),
                    StructuresDestroyed = table.Column<int>(type: "integer", nullable: true),
                    FirefightersDeployed = table.Column<int>(type: "integer", nullable: true),
                    DurationHours = table.Column<double>(type: "double precision", nullable: true),
                    FwiConditionsJson = table.Column<string>(type: "jsonb", nullable: true),
                    WeatherConditionsJson = table.Column<string>(type: "jsonb", nullable: true),
                    TimelineJson = table.Column<string>(type: "jsonb", nullable: true),
                    GeneratedContent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FireReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FireReports_GeoEvents_GeoEventId",
                        column: x => x.GeoEventId,
                        principalTable: "GeoEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FwiForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GridPointId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Location = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false),
                    ForecastDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HorizonDays = table.Column<int>(type: "integer", nullable: false),
                    FFMC = table.Column<double>(type: "double precision", nullable: false),
                    DMC = table.Column<double>(type: "double precision", nullable: false),
                    DC = table.Column<double>(type: "double precision", nullable: false),
                    ISI = table.Column<double>(type: "double precision", nullable: false),
                    BUI = table.Column<double>(type: "double precision", nullable: false),
                    FWI = table.Column<double>(type: "double precision", nullable: false),
                    RiskLevel = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FwiForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Endpoint = table.Column<string>(type: "text", nullable: false),
                    P256dh = table.Column<string>(type: "text", nullable: false),
                    Auth = table.Column<string>(type: "text", nullable: false),
                    DeviceType = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Schedule = table.Column<string>(type: "text", nullable: false),
                    Recipients = table.Column<List<string>>(type: "text[]", nullable: false),
                    LastGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastContent = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeasonalStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: false),
                    TotalFires = table.Column<int>(type: "integer", nullable: false),
                    TotalAreaHa = table.Column<double>(type: "double precision", nullable: false),
                    LargestFireHa = table.Column<double>(type: "double precision", nullable: false),
                    AverageFwi = table.Column<double>(type: "double precision", nullable: false),
                    AverageTemperature = table.Column<double>(type: "double precision", nullable: false),
                    TotalPrecipitationMm = table.Column<double>(type: "double precision", nullable: false),
                    PeakFireDay = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FireCauseBreakdown = table.Column<string>(type: "jsonb", nullable: true),
                    DailyFireCounts = table.Column<string>(type: "jsonb", nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonalStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "terrain_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grid_point_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    location = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false),
                    elevation_meters = table.Column<double>(type: "double precision", nullable: false),
                    slope_degrees = table.Column<double>(type: "double precision", nullable: false),
                    aspect_degrees = table.Column<double>(type: "double precision", nullable: false),
                    solar_exposure_index = table.Column<double>(type: "double precision", nullable: false),
                    terrain_complexity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    north_facing_percent = table.Column<double>(type: "double precision", nullable: false),
                    terrain_risk_score = table.Column<double>(type: "double precision", nullable: false),
                    terrain_fire_risk_contribution = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terrain_analyses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "HotspotAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FireHotspotId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeoEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    AlertType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotspotAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HotspotAlerts_FireHotspots_FireHotspotId",
                        column: x => x.FireHotspotId,
                        principalTable: "FireHotspots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HotspotAlerts_GeoEvents_GeoEventId",
                        column: x => x.GeoEventId,
                        principalTable: "GeoEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NotificationQueueItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertId = table.Column<Guid>(type: "uuid", nullable: true),
                    PushSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CitizenSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationQueueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationQueueItems_Alerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "Alerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NotificationQueueItems_CitizenAlertSubscriptions_CitizenSub~",
                        column: x => x.CitizenSubscriptionId,
                        principalTable: "CitizenAlertSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NotificationQueueItems_PushSubscriptions_PushSubscriptionId",
                        column: x => x.PushSubscriptionId,
                        principalTable: "PushSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AlertRuleId",
                table: "Alerts",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_EscalatedFromAlertId",
                table: "Alerts",
                column: "EscalatedFromAlertId");

            migrationBuilder.CreateIndex(
                name: "IX_AiContextCaches_EventId",
                table: "AiContextCaches",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenAlertSubscriptions_Email",
                table: "CitizenAlertSubscriptions",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenAlertSubscriptions_IsActive",
                table: "CitizenAlertSubscriptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenAlertSubscriptions_Phone",
                table: "CitizenAlertSubscriptions",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_FireHotspots_GridCellId",
                table: "FireHotspots",
                column: "GridCellId");

            migrationBuilder.CreateIndex(
                name: "IX_FireHotspots_Location",
                table: "FireHotspots",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_FireHotspots_RiskLevel",
                table: "FireHotspots",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_FireReports_GeoEventId",
                table: "FireReports",
                column: "GeoEventId");

            migrationBuilder.CreateIndex(
                name: "IX_FwiForecasts_ForecastDate",
                table: "FwiForecasts",
                column: "ForecastDate");

            migrationBuilder.CreateIndex(
                name: "IX_FwiForecasts_ForecastDate_HorizonDays",
                table: "FwiForecasts",
                columns: new[] { "ForecastDate", "HorizonDays" });

            migrationBuilder.CreateIndex(
                name: "IX_FwiForecasts_GridPointId",
                table: "FwiForecasts",
                column: "GridPointId");

            migrationBuilder.CreateIndex(
                name: "IX_FwiForecasts_HorizonDays",
                table: "FwiForecasts",
                column: "HorizonDays");

            migrationBuilder.CreateIndex(
                name: "IX_FwiForecasts_Location",
                table: "FwiForecasts",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_HotspotAlerts_AlertType",
                table: "HotspotAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_HotspotAlerts_CreatedAt",
                table: "HotspotAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HotspotAlerts_FireHotspotId",
                table: "HotspotAlerts",
                column: "FireHotspotId");

            migrationBuilder.CreateIndex(
                name: "IX_HotspotAlerts_GeoEventId",
                table: "HotspotAlerts",
                column: "GeoEventId");

            migrationBuilder.CreateIndex(
                name: "IX_HotspotAlerts_IsRead",
                table: "HotspotAlerts",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueueItems_AlertId",
                table: "NotificationQueueItems",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueueItems_CitizenSubscriptionId",
                table: "NotificationQueueItems",
                column: "CitizenSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueueItems_CreatedAt",
                table: "NotificationQueueItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueueItems_PushSubscriptionId",
                table: "NotificationQueueItems",
                column: "PushSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueueItems_Status",
                table: "NotificationQueueItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_IsActive",
                table: "PushSubscriptions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_UserId",
                table: "PushSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_UserId_IsActive",
                table: "PushSubscriptions",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_terrain_analyses_grid_point_id",
                table: "terrain_analyses",
                column: "grid_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_terrain_analyses_location",
                table: "terrain_analyses",
                column: "location");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_AlertRules_AlertRuleId",
                table: "Alerts",
                column: "AlertRuleId",
                principalTable: "AlertRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Alerts_EscalatedFromAlertId",
                table: "Alerts",
                column: "EscalatedFromAlertId",
                principalTable: "Alerts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_AlertRules_AlertRuleId",
                table: "Alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Alerts_EscalatedFromAlertId",
                table: "Alerts");

            migrationBuilder.DropTable(
                name: "AiContextCaches");

            migrationBuilder.DropTable(
                name: "DashboardCaches");

            migrationBuilder.DropTable(
                name: "FireReports");

            migrationBuilder.DropTable(
                name: "FwiForecasts");

            migrationBuilder.DropTable(
                name: "HotspotAlerts");

            migrationBuilder.DropTable(
                name: "NotificationQueueItems");

            migrationBuilder.DropTable(
                name: "ScheduledReports");

            migrationBuilder.DropTable(
                name: "SeasonalStatistics");

            migrationBuilder.DropTable(
                name: "terrain_analyses");

            migrationBuilder.DropTable(
                name: "FireHotspots");

            migrationBuilder.DropTable(
                name: "CitizenAlertSubscriptions");

            migrationBuilder.DropTable(
                name: "PushSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_AlertRuleId",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_EscalatedFromAlertId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "AlertRuleId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "AreaKm2",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "EscalatedFromAlertId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "FwiValue",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "IsEscalated",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "WindSpeed",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "AreaKm2Threshold",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "ConsecutiveCount",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "EscalationMinutes",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "MaxFwi",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "MinFwi",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "MinTemperature",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "MinWindSpeed",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "NotifyRoles",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "SeasonEndMonth",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "SeasonStartMonth",
                table: "AlertRules");
        }
    }
}
