# Phase 3: Core Domain - GeoEvents Design

**Date:** 2026-05-24
**Status:** Approved
**Depends on:** Phase 1 (Foundation), Phase 2 (Auth)

## Overview

Implement the core GeoEvents domain: CRUD operations, spatial queries via PostGIS, Redis caching, and seed data. Events are immutable records (no update/delete) sourced from manual entry or bulk import.

## Architecture

Vertical Slice pattern (established in Phase 2): `Command/Query record` + `Handler` + `Endpoint extension method`, resolved via Scrutor assembly scanning.

## Files to Create

```
src/GeoRisk.API/
├── Features/Events/
│   ├── Dto/
│   │   └── EventDtos.cs              # CreateEventRequest, EventResponse, EventListResponse, ImportEventItem
│   ├── CreateEvent.cs                 # POST /api/events
│   ├── GetEvents.cs                   # GET /api/events (paginated, filtered, spatial)
│   ├── GetEventById.cs                # GET /api/events/{id}
│   └── ImportEvents.cs                # POST /api/events/import (bulk)
├── Infrastructure/Cache/
│   ├── ICacheService.cs               # GetAsync, SetAsync, RemoveAsync, RemoveByPrefixAsync
│   └── RedisCacheService.cs           # StackExchange.Redis implementation
└── Infrastructure/Persistence/
    └── SeedData.cs                    # ~20 sample events around Seixal
```

## API Design

### GET /api/events

Single endpoint with all filters as optional query params:

| Param | Type | Default | Description |
|-------|------|---------|-------------|
| page | int | 1 | Page number |
| pageSize | int | 20 | Items per page (max 100) |
| type | string | null | EventType enum filter |
| severity | string | null | RiskLevel enum filter |
| from | datetime | null | OccurredAt >= |
| to | datetime | null | OccurredAt <= |
| bbox | string | null | Bounding box: x1,y1,x2,y2 (WGS84) |
| lat | double | null | Proximity center latitude |
| lng | double | null | Proximity center longitude |
| radius | double | null | Proximity radius in meters |
| insideSeixal | bool | null | Geofencing: only events inside Seixal boundary |

Response:
```json
{
  "items": [EventResponse],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20
}
```

### GET /api/events/{id}

Returns single `EventResponse` or 404.

### POST /api/events

Create a single event. Request:
```json
{
  "eventType": "Fire",
  "title": "Incendio na Serra",
  "description": "...",
  "latitude": 38.64,
  "longitude": -9.10,
  "severity": "High",
  "source": "Manual",
  "occurredAt": "2026-05-24T14:00:00Z",
  "metadata": { "area": "5ha" }
}
```

### POST /api/events/import

Bulk import array of events. Request:
```json
[
  {
    "sourceId": "ICNF-2026-001",
    "eventType": "Fire",
    "title": "...",
    "latitude": 38.64,
    "longitude": -9.10,
    "severity": "High",
    "source": "ICNF",
    "occurredAt": "2026-05-24T14:00:00Z"
  }
]
```

Deduplicates by `SourceId` — skips events that already exist.

## Spatial Queries (PostGIS)

### BBox Filter
```sql
WHERE ST_Within(e.Geometry, ST_MakeEnvelope(x1, y1, x2, y2, 4326))
```

### Proximity Search
```sql
WHERE ST_DWithin(
    e.Geometry::geography,
    ST_SetSRID(ST_MakePoint(@lng, @lat), 4326)::geography,
    @radius
)
ORDER BY ST_Distance(e.Geometry::geography, @point)
```

### Geofencing (Seixal Boundary)
```sql
WHERE ST_Contains(@seixalBoundary, e.Geometry)
```

The Seixal boundary polygon is loaded from a constant (simplified GeoJSON polygon) in the handler.

## Caching Strategy

- **Cache key:** `events:{hash_of_all_query_params}`
- **TTL:** 5 minutes
- **Invalidation:** Cache cleared on Create and Import
- **Abstraction:** `ICacheService` interface — Redis in production, in-memory fallback for tests
- **NuGet:** Microsoft.Extensions.Caching.StackExchangeRedis

## Authorization

| Endpoint | Required Role |
|----------|--------------|
| GET /api/events | Any authenticated user (Viewer+) |
| GET /api/events/{id} | Any authenticated user (Viewer+) |
| POST /api/events | Analyst, Admin |
| POST /api/events/import | Admin only |

## Seed Data

~20 realistic events around Seixal municipality center (38.64, -9.10):
- Covers all 7 EventType values (Fire, Flood, Storm, Landslide, Industrial, Heatwave, Other)
- Covers all 4 RiskLevel values (Low, Medium, High, Critical)
- Coordinates spread across Seixal area
- Dates within the last 90 days
- Realistic Portuguese titles and descriptions

## Verification

1. `POST /api/events` creates event with Point geometry
2. `GET /api/events` with no filters returns paginated list
3. `GET /api/events?type=Fire&severity=High` filters correctly
4. `GET /api/events?bbox=-9.2,38.5,-9.0,38.7` returns bbox-filtered events
5. `GET /api/events?lat=38.64&lng=-9.10&radius=5000` returns proximity-sorted events
6. `GET /api/events?insideSeixal=true` geofences correctly
7. `POST /api/events/import` bulk imports with deduplication
8. Cache: second identical query hits Redis
9. Unauthorized access returns 401
10. Non-admin import returns 403
