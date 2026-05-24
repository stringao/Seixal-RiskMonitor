# SeixalRisk Monitor - GeoRisk AI Platform

Plataforma full-stack de monitorização de riscos geográficos para o Concelho do Seixal, com análise espacial, mapa interativo e insights gerados por IA.

## Arquitetura

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   Next.js 15    │────▶│  .NET 10 API     │────▶│ PostgreSQL 17   │
│   TypeScript    │     │  Vertical Slice  │     │ + PostGIS 3.5   │
│   Leaflet       │     │  MediatR + CQRS  │     └─────────────────┘
└─────────────────┘     └──────┬───────────┘
                               │           ┌─────────────────┐
                               ├──────────▶│   Redis 7        │
                               │           └─────────────────┘
                               │           ┌─────────────────┐
                               └──────────▶│  LLM Providers  │
                                           │ Claude / OpenAI  │
                                           └─────────────────┘
```

## Stack

| Camada | Tecnologia |
|--------|-----------|
| Backend | .NET 10, ASP.NET Core Web API |
| Arquitetura | Vertical Slice (MediatR) |
| Base de dados | PostgreSQL 17 + PostGIS 3.5 |
| Cache | Redis 7 |
| Frontend | Next.js 15 + TypeScript + Tailwind CSS |
| Mapa | Leaflet + react-leaflet |
| IA | Multi-provider (Claude API + OpenAI) |
| Auth | JWT com refresh tokens |
| Infra | Docker Compose |

## Funcionalidades

### Mapa Interativo
- Visualização de eventos geográficos em tempo real
- Polígonos de zonas de risco (PostGIS)
- Filtros por tipo, severidade e data
- Buffer zones à volta de eventos críticos
- Boundary do Concelho do Seixal (GeoJSON)

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

## Quick Start

```bash
# Clonar o repositório
git clone https://github.com/ricardo-oliveira/SeixalRiscalMonitor.git
cd SeixalRiscalMonitor

# Configurar environment variables
cp .env.example .env

# Iniciar todos os serviços
docker compose up -d

# Aceder à aplicação
# Frontend: http://localhost:3000
# API:      http://localhost:5000/swagger
```

## Estrutura do Projeto

```
SeixalRiscalMonitor/
├── src/
│   └── GeoRisk.API/            # Backend .NET 10 (projeto único)
│       ├── Features/            # Vertical Slices por feature
│       ├── Infrastructure/      # PostGIS, Redis, AI, External APIs
│       ├── Domain/              # Entities, Enums
│       ├── Auth/                # JWT
│       └── BackgroundJobs/      # Import, Classification, Alerts
├── web/                         # Frontend Next.js
│   ├── app/
│   ├── components/
│   └── lib/
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
| Insights | `POST /api/insights/classify/{id}`, `/report`, `GET /patterns` |
| Alerts | `GET/PUT /api/alerts`, `POST /api/alerts/rules` |

## Requisitos

- Docker & Docker Compose
- .NET 10 SDK (para desenvolvimento local)
- Node.js 20+ (para desenvolvimento local)
- Chaves de API: Anthropic e/ou OpenAI (para features de IA)

## Licença

MIT
