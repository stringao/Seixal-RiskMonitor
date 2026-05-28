# SeixalRisk Monitor - GeoRisk AI Platform

Plataforma full-stack de monitorização de riscos geográficos para o Concelho do Seixal, com análise espacial, mapa interativo, qualidade do ar e insights gerados por IA.

## Arquitetura

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   Next.js 16    │────▶│  .NET 10 API     │────▶│ PostgreSQL 17   │
│   TypeScript    │     │  Minimal APIs    │     │ + PostGIS 3.5   │
│   Leaflet       │     │  Vertical Slice  │     └─────────────────┘
└─────────────────┘     └──────┬───────────┘
                               │           ┌─────────────────┐
                               ├──────────▶│   Redis 7        │
                               │           └─────────────────┘
                               │           ┌─────────────────┐
                               ├──────────▶│  LLM Providers   │
                               │           │ Claude / OpenAI  │
                               │           └─────────────────┘
                               │           ┌─────────────────┐
                               ├──────────▶│  OSRM Routing    │
                               │           │ Self-hosted     │
                               │           └─────────────────┘
                               │           ┌─────────────────┐
                               └──────────▶│ Open-Meteo API   │
                                           │ Air Quality +    │
                                           │ Pollen Data      │
                                           └─────────────────┘
```

## Stack

| Camada | Tecnologia |
|--------|-----------|
| Backend | .NET 10, ASP.NET Core Web API |
| Arquitetura | Vertical Slice (MediatR) |
| Base de dados | PostgreSQL 17 + PostGIS 3.5 |
| Cache | Redis 7 |
| Frontend | Next.js 16 + TypeScript + Tailwind CSS |
| Mapa | Leaflet + react-leaflet (OpenStreetMap) |
| Routing | OSRM self-hosted (OpenStreetMap data) |
| Qualidade do Ar | Open-Meteo Air Quality API (PM10, PM2.5, NO₂, O₃, SO₂, CO, polén) |
| IA | Multi-provider (Ollama, Claude API, DeepSeek, Qwen, OpenAI) |
| Auth | JWT com refresh tokens |
| Infra | Docker Compose |

## Funcionalidades

### Mapa Interativo
- Visualização de eventos geográficos em tempo real
- Polígonos de zonas de risco (PostGIS)
- Filtros por tipo, severidade e data
- Buffer zones à volta de eventos críticos
- Boundary do Concelho do Seixal (GeoJSON)
- **Routing automático** de ocorrências para bombeiros via OSRM (estradas reais)
- Mapa OpenStreetMap com tiles e маркеры

### Dashboard de Eventos
- Lista paginada de eventos (5, 10 ou 25 por página)
- Detalhe de evento com mapa split + informações
- Filtros por tipo, severidade e unread

### Análise Espacial (PostGIS)
- Interseção entre eventos e zonas de risco
- Proximidade de eventos a pontos de interesse
- Geofencing do concelho
- Cálculo de buffers para alertas

### IA Aplicada
- **Classificação automática** de incidentes por tipo e gravidade
- **Relatórios** em linguagem natural (periódicos ou manuais)
- **Detecção de padrões** em dados históricos
- **Alertas inteligentes** com recomendações contextuais

### Dados Reais
- ICNF - Incêndios florestais
- IPMA - Dados meteorológicos
- ANEPC - Emergências e proteção civil
- CAOP - Limites administrativos
- Open-Meteo - Qualidade do ar e dados de polén (European AQI)

### Qualidade do Ar & Poluição
- **Página dedicada** com dados em tempo real do Seixal
- **Mapa interativo** com estação de monitorização, círculo de influência e popup com detalhes
- Índice AQI europeu (1-5): PM10, PM2.5, NO₂, O₃, SO₂, CO
- Índice de polén por espécie (erva, oliveira, bétula, etc.)
- Previsão de 5 dias com gráfico AQI + polén
- Banner de risco para alérgicos (visível quando índices ≥ 3)
- Widget no Dashboard com resumo rápido

## Quick Start

```bash
# Clonar o repositório
git clone https://github.com/stringao/Seixal-RiskMonitor.git
cd Seixal-RiskMonitor

# Configurar environment variables
cp .env.example .env

# Preparar dados OSM para routing (apenas na primeira vez - demora ~15min)
bash scripts/setup-osrm-data.sh

# Iniciar todos os serviços (inclui OSRM para routing)
docker compose up -d

# Aceder à aplicação
# Frontend: http://localhost:3000
# API:      http://localhost:5000/swagger
# OSRM:     http://localhost:5001 (para debugging)
```

## Estrutura do Projeto

```
SeixalRiscalMonitor/
├── src/
│   └── GeoRisk.API/            # Backend .NET 10 (projeto único)
│       ├── Features/            # Vertical Slices por feature
│       │   ├── Environment/     # Air Quality + Pollen endpoints
│       │   ├── Risk/            # Risk zones, terrain
│       │   ├── FireSpread/      # Fire spread simulation
│       │   ├── Notifications/   # Push notifications
│       │   └── ...
│       ├── Infrastructure/      # PostGIS, Redis, AI, External APIs
│       │   ├── ExternalApis/    # OpenMeteo client (weather + air quality)
│       │   └── Services/        # AirQualityService, FireSpreadCalculator
│       ├── Domain/              # Entities, Enums
│       ├── Auth/                # JWT
│       └── BackgroundJobs/      # Import, Classification, Alerts
├── web/                         # Frontend Next.js 16
│   ├── app/
│   │   ├── dashboard/           # Dashboard + events list/detail
│   │   ├── air-quality/         # Qualidade do Ar (mapa + dados)
│   │   ├── map/                 # Mapa interativo
│   │   ├── terrain/             # Terreno + declives
│   │   ├── risk-zones/          # Zonas de risco
│   │   ├── fire-spread/         # Simulação de propagação de fogo
│   │   ├── alerts/              # Alertas + regras
│   │   ├── insights/            # Relatórios IA
│   │   └── settings/            # Definições
│   ├── components/
│   │   ├── map/                 # MapContainer, StaticMap, EventMarker
│   │   ├── environment/         # AirQualityCard, PollenCard, AirQualityMap, Chart
│   │   ├── dashboard/           # StatsCards, Charts
│   │   ├── alerts/              # AlertList, AlertCard, AlertBadge
│   │   └── ui/                  # Pagination, etc
│   └── lib/
│       ├── api/                 # API clients (environment, risk, etc)
│       ├── types/               # TypeScript types + adapters
│       ├── hooks/               # useAirQuality, usePollen, useEvents
│       └── map/                 # Map config (Seixal center)
├── scripts/
│   └── setup-osrm-data.sh       # Script para preparar dados OSM
├── docker/
│   └── postgres/                # Init SQL (PostGIS extension)
├── docker-compose.yml
├── .env.example
└── README.md
```

## API Endpoints

| Área | Endpoints |
|------|-----------|
| Auth | `POST /api/auth/register`, `login`, `refresh` |
| Events | `GET/POST /api/events`, `POST /api/events/import` |
| Risk | `GET /api/risk/zones`, `/dashboard`, `POST /calculate` |
| Environment | `GET /api/environment/air-quality/{lat}/{lon}`, `/pollen/{lat}/{lon}`, `/air-quality/region` |
| Insights | `POST /api/insights/classify/{id}`, `/report`, `GET /patterns` |
| Alerts | `GET /api/alerts?page=&pageSize=&severity=&isRead=` |

## Requisitos

- Docker & Docker Compose
- .NET 10 SDK (para desenvolvimento local)
- Node.js 20+ (para desenvolvimento local)
- Espaço em disco: ~500MB para dados OSM de Portugal
- Chaves de API: Anthropic e/ou OpenAI (para features de IA)

## Environment Variables

| Variável | Descrição |
|----------|-----------|
| `NEXT_PUBLIC_OSRM_URL` | URL do serviço OSRM (default: http://localhost:5001) |
| `OPENROUTESERVICE_API_KEY` | Chave API OpenRouteService (opcional, para fallback) |
| `ANTHROPIC_API_KEY` | Chave API Anthropic Claude |
| `OPENAI_API_KEY` | Chave API OpenAI |

## Licença

MIT
