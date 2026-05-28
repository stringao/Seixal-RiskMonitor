# Manual do Utilizador — Seixal Risk Monitor

**Versão:** 1.0  
**Data:** Maio 2026  
**Plataforma:** Aplicação Web (Docker)

---

## Índice

1. [Introdução](#1-introdução)
2. [Requisitos e Instalação](#2-requisitos-e-instalação)
3. [Autenticação](#3-autenticação)
4. [Painel Principal (Dashboard)](#4-painel-principal-dashboard)
5. [Mapa Interativo](#5-mapa-interativo)
6. [Análise de Terreno](#6-análise-de-terreno)
7. [Zonas de Risco](#7-zonas-de-risco)
8. [Simulação de Propagação de Incêndio](#8-simulação-de-propagação-de-incêndio)
9. [Qualidade do Ar](#9-qualidade-do-ar)
10. [Insights e Análises](#10-insights-e-análises)
11. [Alertas](#11-alertas)
12. [Configurações](#12-configurações)
13. [Suporte Técnico](#13-suporte-técnico)

---

## 1. Introdução

O **Seixal Risk Monitor** é uma plataforma de monitorização e gestão de riscos geográficos para o concelho do Seixal. A aplicação integra dados de qualidade do ar, eventos geográficos, análise de terreno e simulação de propagação de incêndios numa interface web unificada com mapas interativos.

### Funcionalidades Principais

- **Painel de Controlo** — Visão geral em tempo real dos indicadores de risco
- **Mapa Interativo** — Visualização georreferenciada de eventos e zonas de risco
- **Análise de Terreno** — Modelo digital de elevação e análise topográfica
- **Zonas de Risco** — Identificação e classificação de áreas de perigo
- **Simulação de Incêndios** — Modelação da propagação de fogo baseada em condições meteorológicas
- **Qualidade do Ar** — Monitorização em tempo real do Índice de Qualidade do Ar (IQA), poluentes e pólenes
- **Insights** — Análises preditivas e relatórios automatizados
- **Alertas** — Sistema de notificações configurável por regras

### Arquitetura

| Componente | Tecnologia | Porta |
|---|---|---|
| Frontend (Web) | Next.js 14 + React + Tailwind CSS | 3000 |
| Backend (API) | .NET 8 + Minimal APIs | 5000 |
| Base de Dados | PostgreSQL + PostGIS | 5432 |
| Cache | Redis | 6379 |
| Roteamento | OSRM (Open Source Routing Machine) | 5001 |

---

## 2. Requisitos e Instalação

### Requisitos

- **Docker** 24+ e **Docker Compose** v2+
- **RAM:** mínimo 8 GB (recomendado 16 GB)
- **Disco:** mínimo 10 GB livres
- **Rede:** acesso à Internet (dados de Open-Meteo API)

### Instalação via Docker

1. Clone o repositório:
   ```bash
   git clone https://github.com/stringao/Seixal-RiskMonitor.git
   cd Seixal-RiskMonitor
   git checkout feature/heat-risk-fire-spread
   ```

2. Configure as variáveis de ambiente (copie e edite o ficheiro `.env`):
   ```bash
   cp .env.example .env
   ```

3. Inicie todos os serviços:
   ```bash
   docker compose up -d
   ```

4. Aguarde que todos os containers estejam saudáveis:
   ```bash
   docker compose ps
   ```

5. Aceda à aplicação em: **http://localhost:3000**

### Verificação dos Serviços

| URL | Descrição |
|---|---|
| http://localhost:3000 | Interface Web |
| http://localhost:5000/swagger | Documentação da API |
| http://localhost:5000/health | Estado de saúde da API |

---

## 3. Autenticação

### Acesso Inicial

Aceda a **http://localhost:3000/login** para iniciar sessão.

![Página de Login](screenshots/01-login-page.png)

### Iniciar Sessão

1. Introduza o seu **email** e **palavra-passe**
2. Clique em **Entrar**

![Login Preenchido](screenshots/02-login-filled.png)

Após autenticação bem-sucedida, será redirecionado para o Painel Principal.

### Registo de Novo Utilizador

Caso não tenha credenciais, clique no link **Criar conta** na página de login e preencha os dados solicitados.

---

## 4. Painel Principal (Dashboard)

O Dashboard é a página inicial após o login e apresenta uma visão geral consolidada de todos os indicadores de risco do concelho do Seixal.

![Dashboard](screenshots/03-dashboard.png)

### Componentes do Dashboard

#### 4.1 Indicadores de Risco (Topo)

Cartões com métricas-chave atualizadas em tempo real:

- **Eventos Ativos** — Número de eventos geográficos em curso
- **Nível de Risco** — Classificação atual do risco global
- **Área Monitorizada** — Extensão territorial sob vigilância
- **Alertas Pendentes** — Notificações por resolver

#### 4.2 Mapa de Resumo

Mapa simplificado com a localização dos eventos ativos e zonas de risco, centrado no Seixal (38.6267°N, 9.1048°W).

#### 4.3 Painel de Qualidade do Ar

Widget integrado no dashboard que mostra o IQA atual, nível de saúde e principais poluentes:

![Dashboard — Qualidade do Ar](screenshots/04-dashboard-air-quality.png)

O painel inclui:
- **Índice IQA** — Valor numérico com classificação por cores
- **Nível de Risco de Saúde** — Indicador de risco para a população
- **Previsão** — Gráfico de evolução do IQA para as próximas horas

---

## 5. Mapa Interativo

O mapa interativo é a ferramenta principal de visualização geográfica, permitindo explorar eventos, zonas de risco e dados ambientais sobrepostos ao mapa do concelho.

![Mapa Interativo](screenshots/05-map.png)

### Funcionalidades

- **Navegação** — Arraste para mover, scroll para zoom, duplo clique para zoom rápido
- **Marcadores de Eventos** — Ícones coloridos por tipo e gravidade
- **Pop-ups Detalhados** — Clique num marcador para ver detalhes do evento

![Popup de Evento](screenshots/06-map-event-popup.png)

### Camadas Disponíveis

O mapa suporta múltiplas camadas que podem ser ativadas/desativadas:

| Camada | Descrição |
|---|---|
| Eventos | Localização de eventos geográficos |
| Zonas de Risco | Áreas classificadas por nível de perigo |
| Heatmap | Mapa de calor de densidade de eventos |
| Terreno | Modelo de elevação do terreno |

---

## 6. Análise de Terreno

A página de Análise de Terreno apresenta um modelo digital de elevação (MDE) do concelho, permitindo avaliar as características topográficas relevantes para a gestão de riscos.

![Análise de Terreno](screenshots/07-terrain.png)

### Informações Disponíveis

- **Altitude** — Elevação do terreno em metros
- **Declive** — Inclinação do terreno em graus ou percentagem
- **Aspecto** — Orientação do terreno (Norte, Sul, Este, Oeste)
- **Perfis Topográficos** — Cortes transversais do terreno

### Utilização

1. Navegue pelo mapa para explorar diferentes áreas
2. Clique num ponto para ver as métricas detalhadas de terreno
3. Use os controlos de camada para alternar entre visualizações

---

## 7. Zonas de Risco

Esta página identifica e classifica as áreas do concelho com diferentes níveis de risco, permitindo uma análise espacial detalhada.

![Zonas de Risco](screenshots/08-risk-zones.png)

### Classificação de Risco

As zonas são categorizadas por níveis:

| Nível | Cor | Descrição |
|---|---|---|
| Baixo | 🟢 Verde | Risco mínimo, sem restrições |
| Moderado | 🟡 Amarelo | Risco moderado, vigilância recomendada |
| Alto | 🟠 Laranja | Risco significativo, restrições podem aplicar-se |
| Muito Alto | 🔴 Vermelho | Risco crítico, intervenção prioritária |

### Funcionalidades

- Visualização de polígonos de risco sobre o mapa
- Filtros por tipo de risco (incêndio, inundação, deslizamento)
- Detalhes de cada zona ao clicar
- Exportação de dados para análise externa

---

## 8. Simulação de Propagação de Incêndio

A simulação de propagação de incêndio permite modelar o comportamento do fogo com base em condições meteorológicas e características do terreno.

![Simulação de Incêndio](screenshots/09-fire-spread.png)

### Parâmetros de Simulação

| Parâmetro | Descrição |
|---|---|
| Ponto de origem | Localização inicial do incêndio |
| Velocidade do vento | Intensidade do vento em km/h |
| Direção do vento | Rumo do vento em graus |
| Humidade | Humidade relativa do ar |
| Temperatura | Temperatura ambiente |
| Tipo de vegetação | Coberto vegetal da área |

### Resultados da Simulação

A simulação apresenta:
- **Área afetada** — Perímetro estimado de propagação
- **Tempo de propagação** — Evolução temporal do fogo
- **Velocidade de propagação** — Taxa de avanço em diferentes direções
- **Rotas de evacuação** — Vias de fuga calculadas via OSRM

### Como Utilizar

1. Selecione um evento de incêndio existente ou defina um ponto de origem
2. Ajuste os parâmetros meteorológicos
3. Clique em **Simular** para iniciar a modelação
4. Analise os resultados sobre o mapa

---

## 9. Qualidade do Ar

A página de Qualidade do Ar fornece monitorização em tempo real do ar ambiente no concelho do Seixal, incluindo poluentes atmosféricos e níveis de pólen.

![Qualidade do Ar](screenshots/11-air-quality.png)

### 9.1 Índice de Qualidade do Ar (IQA)

O IQA é calculado com base nos seguintes poluentes:

| Poluente | Unidade | Descrição |
|---|---|---|
| PM2.5 | µg/m³ | Partículas finas |
| PM10 | µg/m³ | Partículas inaláveis |
| O₃ | µg/m³ | Ozono troposférico |
| NO₂ | µg/m³ | Dióxido de azoto |
| SO₂ | µg/m³ | Dióxido de enxofre |
| CO | µg/m³ | Monóxido de carbono |

### 9.2 Classificação do IQA

| IQA | Nível | Cor | Risco de Saúde |
|---|---|---|---|
| 0–20 | Muito Bom | 🟢 | Risco baixo |
| 20–40 | Bom | 🟢 | Risco baixo |
| 40–60 | Médio | 🟡 | Risco moderado para grupos sensíveis |
| 60–80 | Mau | 🟠 | Risco para a saúde |
| 80–100 | Muito Mau | 🔴 | Risco elevado para toda a população |

### 9.3 Mapa de Qualidade do Ar

Mapa interativo com a estação de monitorização do Seixal, mostrando a posição exata do ponto de medição com um marcador IQA e respetivo círculo de influência:

![Mapa de Qualidade do Ar](screenshots/12-air-quality-map.png)

Ao clicar no marcador, surge um popup com os detalhes dos poluentes medidos:

![Popup do Marcador IQA](screenshots/13-air-quality-map-popup.png)

### 9.4 Detalhes dos Poluentes

Secção detalhada com os valores individuais de cada poluente, comparados com os limites legais:

![Detalhes dos Poluentes](screenshots/14-air-quality-pollutants.png)

### 9.5 Pólenes e Alergias

A página inclui também informação sobre níveis de pólen:

- **Índice de Pólen** — Classificação geral (Baixo a Muito Alto)
- **Pólenes Dominantes** — Gramíneas, Oliveira, Bétula, etc.
- **Distribuição por Espécie** — Percentagem de cada tipo de pólen
- **Recomendações de Saúde** — Orientações para pessoas com alergias

### Fonte de Dados

Os dados de qualidade do ar são obtidos em tempo real da **Open-Meteo Air Quality API**, atualizados a cada hora.

---

## 10. Insights e Análises

A página de Insights apresenta análises preditivas, tendências e relatórios automatizados sobre os riscos monitorizados.

![Insights](screenshots/15-insights.png)

### Conteúdos

- **Tendências de Risco** — Evolução temporal dos indicadores
- **Previsões** — Projeções baseadas em modelos meteorológicos
- **Relatórios Automatizados** — Resumos periódicos da atividade
- **Recomendações** — Sugestões de ação baseadas na análise de dados

---

## 11. Alertas

O sistema de alertas permite configurar notificações automáticas baseadas em condições pré-definidas, garantindo uma resposta atempada a situações de risco.

![Alertas](screenshots/16-alerts.png)

### 11.1 Gestão de Alertas

- **Alertas Ativos** — Notificações geradas pelo sistema
- **Histórico** — Registo de alertas passados
- **Filtros** — Por tipo, gravidade, data e estado

### 11.2 Regras de Alerta

As regras definem as condições que acionam as notificações:

![Regras de Alerta](screenshots/17-alert-rules.png)

#### Configuração de uma Regra

1. Aceda a **Alertas > Regras**
2. Clique em **Nova Regra**
3. Defina os parâmetros:
   - **Tipo de evento** (incêndio, inundação, qualidade do ar, etc.)
   - **Condição** (limiar de ativação)
   - **Zona geográfica** (área de aplicação)
   - **Gravidade** (informação, aviso, crítico)
   - **Canais de notificação** (email, push, SMS)
4. Guarde a regra

---

## 12. Configurações

A página de Configurações permite personalizar o comportamento da aplicação e gerir a conta do utilizador.

![Configurações](screenshots/18-settings.png)

### Opções Disponíveis

- **Perfil** — Dados pessoais e alteração de palavra-passe
- **Notificações** — Preferências de alerta e comunicação
- **Mapa** — Configurações de visualização do mapa (camadas padrão, zoom)
- **API** — Chaves de acesso e integrações externas

---

## Navegação e Interface

### Barra Lateral

A navegação principal é feita através da barra lateral lateral esquerda, que pode ser recolhida para maximizar a área de trabalho:

| Ícone | Página |
|---|---|
| 🏠 | Dashboard |
| 🗺️ | Mapa Interativo |
| ⛰️ | Análise de Terreno |
| 🛡️ | Zonas de Risco |
| 🔥 | Simulação de Incêndio |
| 🌬️ | Qualidade do Ar |
| 📊 | Insights |
| 🔔 | Alertas |
| ⚙️ | Configurações |

### Barra Lateral Recolhida

![Sidebar Recolhida](screenshots/19-sidebar-collapsed.png)

### Versão Móvel

A interface adapta-se automaticamente a dispositivos móveis com layout responsivo:

**Dashboard em móvel:**

![Dashboard Mobile](screenshots/20-mobile-dashboard.png)

**Qualidade do Ar em móvel:**

![Qualidade do Ar Mobile](screenshots/21-mobile-air-quality.png)

---

## 13. Suporte Técnico

### Resolução de Problemas Comuns

| Problema | Solução |
|---|---|
| A aplicação não carrega | Verifique se os containers Docker estão a correr: `docker compose ps` |
| Dados de qualidade do ar não aparecem | Confirme acesso à Internet (API Open-Meteo) |
| Mapa não renderiza | Atualize a página (Ctrl+F5); verifique a ligação ao tile server |
| Login falha | Verifique credenciais; tente recriar o utilizador via API |
| Erro 500 na API | Consulte os logs: `docker compose logs georisk-api` |

### Comandos Úteis

```bash
# Ver estado dos serviços
docker compose ps

# Ver logs da API
docker compose logs -f georisk-api

# Ver logs do Frontend
docker compose logs -f georisk-web

# Reiniciar todos os serviços
docker compose restart

# Reconstruir após alterações
docker compose build --no-cache georisk-web
docker compose up -d
```

### API Endpoints Principais

| Endpoint | Método | Descrição |
|---|---|---|
| `/api/auth/login` | POST | Autenticação |
| `/api/auth/register` | POST | Registo de utilizador |
| `/api/environment/air-quality` | GET | Dados de qualidade do ar |
| `/api/environment/pollen` | GET | Dados de pólenes |
| `/api/geoevents` | GET | Lista de eventos geográficos |
| `/api/risk-zones` | GET | Zonas de risco |
| `/api/fire-spread/simulate` | POST | Simulação de incêndio |
| `/api/alerts` | GET | Alertas do utilizador |
| `/api/alerts/rules` | GET/POST | Regras de alerta |

Documentação completa da API disponível em: **http://localhost:5000/swagger**

---

> **Seixal Risk Monitor** — Plataforma de Monitorização de Riscos Geográficos  
> © 2026 — Todos os direitos reservados
