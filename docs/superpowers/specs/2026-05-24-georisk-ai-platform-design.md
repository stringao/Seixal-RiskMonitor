# GeoRisk AI Platform - Design Spec

**Date:** 2026-05-24
**Status:** Approved
**Project:** SeixalRiscalMonitor

## Overview

GeoRisk AI Platform is a full-stack portfolio application that monitors geographic events in the Seixal municipality (Portugal), analyzes risk using spatial queries, visualizes data on an interactive map, and generates AI-powered insights.

## Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 10, ASP.NET Core Web API |
| Architecture | Vertical Slice (single project) |
| Database | PostgreSQL 17 + PostGIS 3.5 |
| Cache | Redis 7 |
| Frontend | Next.js 15 + TypeScript + Tailwind CSS |
| Map | Leaflet + react-leaflet |
| AI | Multi-provider abstraction (Claude API + OpenAI) |
| Background Jobs | .NET BackgroundService |
| Infra | Docker Compose |
| Auth | JWT (custom, with refresh tokens) |

## Backend Structure (Single Project)

```
src/GeoRisk.API/
├── Program.cs
├── Features/                       # Vertical Slices
│   ├── Events/
│   │   ├── CreateEvent.cs
│   │   ├── GetEvents.cs
│   │   ├── GetEventsByArea.cs
│   │   └── ImportEvents.cs
│   ├── Risk/
│   │   ├── CalculateRisk.cs
│   │   ├── GetRiskZones.cs
│   │   └── GetRiskDashboard.cs
│   ├── Insights/
│   │   ├── ClassifyIncident.cs
│   │   ├── GenerateReport.cs
│   │   ├── DetectPatterns.cs
│   │   └── GenerateAlert.cs
│   └── Alerts/
│       ├── GetAlerts.cs
│       ├── ConfigureAlertRules.cs
│       └── GetAlertHistory.cs
├── Infrastructure/
│   ├── Persistence/                # DbContext, PostGIS configs
│   ├── Cache/                      # Redis
│   ├── AI/                         # Multi-provider abstraction
│   └── ExternalApis/              # ICNF, IPMA, ANEPC
├── Domain/                         # Entities, Enums, Value Objects
├── Auth/                           # JWT, middleware, policies
├── BackgroundJobs/                 # BackgroundService workers
└── Common/                         # Middleware, extensions, helpers
```

## Data Model

### Entities

**GeoEvent** - Geographic incident
- Id (Guid), EventType (enum), Title, Description
- Geometry (Point - PostGIS), Severity (enum)
- Source (enum: ICNF, IPMA, ANEPC, Manual, AI_Detected)
- OccurredAt, AIClassification, AIInsight (JSONB), Metadata (JSONB)

**RiskZone** - Calculated risk area
- Id (Guid), Name, Geometry (Polygon - PostGIS)
- RiskLevel (enum), CalculatedAt, Source, Metadata (JSONB)

**Alert** - System alert
- Id (Guid), Title, Severity (enum), Message
- GeoEventId (FK), IsRead, CreatedAt

**AlertRule** - Alert configuration
- Id (Guid), Name, EventType (enum?), SeverityThreshold
- Area (Geometry - optional), IsActive, CreatedAt

**User** - Auth user
- Id (Guid), Email, PasswordHash, Role (Admin, Analyst, Viewer)
- CreatedAt, LastLoginAt

### Enums
- EventType: Fire, Flood, Storm, Landslide, Industrial, Heatwave, Other
- RiskLevel: Low, Medium, High, Critical
- EventSource: ICNF, IPMA, ANEPC, Manual, AI_Detected
- AlertSeverity: Info, Warning, Danger, Critical

## Spatial Queries (PostGIS)

1. **Intersection** - Which risk zones contain this event?
2. **Proximity** - Events within X km of a location
3. **Geofencing** - Events inside Seixal municipality boundary
4. **Buffer zones** - Alert zone around critical events

## AI Features

| Feature | Trigger | Output |
|---------|---------|--------|
| Auto-classification | New event imported | EventType + Severity + confidence |
| Reports | Manual or weekly schedule | Natural language summary |
| Pattern detection | Daily schedule | List of detected patterns |
| Smart alerts | High severity event | Alert message + recommendations |

### Multi-Provider Abstraction

```
ILlmProvider (interface)
├── AnthropicProvider     # Claude API
├── OpenAIProvider        # GPT
└── (extensible)
```

Active provider configured via appsettings.json.

## Background Jobs

| Job | Schedule | Purpose |
|-----|----------|---------|
| EventImportJob | Every 30 min | Poll ICNF, IPMA APIs |
| AIClassificationJob | On new events | Classify unprocessed events |
| PatternDetectionJob | Daily | Detect patterns in last 7 days |
| ReportGenerationJob | Weekly | Generate summary report |
| AlertEvaluationJob | Every 5 min | Evaluate alert rules |

## API Endpoints

### Auth
- POST /api/auth/register
- POST /api/auth/login
- POST /api/auth/refresh

### Events
- GET /api/events?bbox=&type=&from=&to=&severity=
- GET /api/events/{id}
- POST /api/events
- POST /api/events/import

### Risk
- GET /api/risk/zones
- GET /api/risk/dashboard
- POST /api/risk/calculate

### Insights (AI)
- POST /api/insights/classify/{eventId}
- POST /api/insights/report
- GET /api/insights/patterns

### Alerts
- `GET /api/alerts?page=&pageSize=&severity=&isRead=` (paginated, filtered)
- `POST /api/alerts/read` (mark read)
- `GET /api/alerts/rules`
- `POST /api/alerts` (create/update rule)
- `PATCH /api/alerts/{id}/toggle`
- `DELETE /api/alerts/{id}`

### Health/Info
- GET /api/health
- GET /api/info/sources

## Frontend Structure

```
web/
├── app/
│   ├── (auth)/          # Login, Register
│   ├── dashboard/       # Main dashboard
│   ├── map/             # Interactive map
│   ├── insights/        # AI reports
│   └── alerts/          # Alert management
├── components/
│   ├── map/             # MapContainer, EventMarker, RiskZone
│   ├── dashboard/       # Stats, Charts
│   ├── insights/        # AIReport, PatternCard
│   └── ui/              # Button, Card, Modal
└── lib/                 # API client, hooks, utils
```

### Map Features (Leaflet)
- OpenStreetMap tile layer
- Event markers (color-coded by severity)
- Risk zone polygons (semi-transparent fill)
- Event detail popups with AI classification
- Filters by type, severity, date
- Buffer visualization around critical events
- Seixal municipality boundary (GeoJSON)

## Docker Compose

```
Services:
- georisk-api        (.NET 10)
- georisk-web        (Next.js)
- georisk-db         (PostgreSQL 17 + PostGIS 3.5)
- georisk-redis      (Redis 7)
```

## Data Sources (Portuguese Public APIs)

- **ICNF** - Institute for Nature Conservation and Forests (fire data)
- **IPMA** - Portuguese Institute for Sea and Atmosphere (weather data)
- **ANEPC** - National Emergency and Civil Protection Authority (emergency data)
- **CAOP** - Official Administrative Map of Portugal (Seixal boundary)

## Geographic Scope

Concelho do Seixal, Portugal. Boundary polygon loaded from CAOP official data.

## Success Criteria

- Demonstrate full-stack competency (.NET + React + AI)
- Real Portuguese data sources integrated
- Interactive map with spatial queries
- AI-powered insights that add real value
- Clean, well-organized codebase suitable for portfolio review
- Fully runnable via Docker Compose
