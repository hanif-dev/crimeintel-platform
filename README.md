# CrimeIntel Platform

> Full-stack intelligence analysis platform for fraud detection and criminal network analysis — built as a portfolio demonstration of production-grade software engineering across multiple technology stacks.

[![CI](https://github.com/hanif-dev/crimeintel-platform/actions/workflows/ci.yml/badge.svg)](https://github.com/hanif-dev/crimeintel-platform/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Python](https://img.shields.io/badge/Python-3.11-3776AB?logo=python)
![Vue.js](https://img.shields.io/badge/Vue.js-3-4FC08D?logo=vuedotjs)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)
![Elasticsearch](https://img.shields.io/badge/Elasticsearch-8.x-005571?logo=elasticsearch)

---

## Overview

CrimeIntel is a multi-service intelligence platform that mirrors real-world law enforcement and financial crime analysis systems. It demonstrates end-to-end software delivery — from data ingestion and ML scoring through to REST APIs, full-text search, and an interactive analyst workspace with network graph visualisation.

The platform is designed to showcase skills directly relevant to roles in international organisations such as Europol, Interpol, financial intelligence units, and RegTech companies.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     CrimeIntel Platform                     │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │           Vue 3 + TypeScript Frontend  :3000          │  │
│  │  Dashboard · Cases · Search · Network Graph · Ingest  │  │
│  └────────────┬──────────────┬──────────────────────────┘  │
│               │              │                              │
│  ┌────────────▼──┐  ┌────────▼────────┐  ┌─────────────┐  │
│  │  .NET 8 API   │  │ Ingestion Svc   │  │  ML Service  │  │
│  │  :5000        │  │ (Python) :8000  │  │ (Python)     │  │
│  │  REST + CRUD  │  │ ETL Pipeline    │  │ :8001        │  │
│  │  EF Core      │  │ CSV/JSON ingest │  │ Isolation    │  │
│  │  Elasticsearch│  │ pandas cleaning │  │ Forest model │  │
│  └──────┬────┬──┘  └────────┬────────┘  └──────┬───────┘  │
│         │    │               │                  │           │
│  ┌──────▼─┐ ┌▼─────────────┐│                  │           │
│  │  SQL   │ │Elasticsearch  ││◄─────────────────┘           │
│  │ Server │ │  8.x  :9200   ││                              │
│  │  2022  │ │  Full-text    ││                              │
│  └────────┘ └───────────────┘│                              │
│                               │                              │
│  ──── Docker Compose ─────────┘                             │
│  ──── GitHub Actions CI/CD ──────────────────────────────  │
└─────────────────────────────────────────────────────────────┘
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| **Backend API** | C# .NET 8, Entity Framework Core 8, Swagger/OpenAPI |
| **Search** | Elasticsearch 8.x |
| **Database** | Microsoft SQL Server 2022 |
| **ETL Service** | Python 3.11, FastAPI, pandas |
| **ML Service** | Python 3.11, FastAPI, scikit-learn (Isolation Forest) |
| **Frontend** | Vue 3, TypeScript, Pinia, Vue Router, Tailwind CSS |
| **Visualisation** | D3.js (network/link analysis graph), Chart.js (analytics) |
| **Infrastructure** | Docker, docker-compose |
| **CI/CD** | GitHub Actions (build, type-check, test, compose validate) |
| **Dev Environment** | GitHub Codespaces (devcontainer) |

---

## Key Features

### Intelligence Case Management
- Full CRUD for cases with risk scoring (0–100), status tracking, and analyst assignment
- Crime categories: Financial Fraud, Cybercrime, Money Laundering, Terrorism Financing, Organised Crime, Trafficking
- Audit trail via analyst notes

### Link Analysis — Network Graph
- D3.js force-directed graph visualising entity relationships
- Entity types: Person, Company, Account, CryptoWallet, IPAddress
- Relationship types: `controls`, `funds`, `communicates_with`
- Interactive: drag nodes, zoom, hover tooltips

### ML Fraud Scoring
- Isolation Forest anomaly detection (scikit-learn)
- 7-feature model: transaction amount, cross-border flag, high-risk country flags, time-of-day, currency risk, round-amount pattern
- Per-feature explainability score on every result
- Batch scoring via REST API for ETL pipeline integration

### Full-Text Search
- Elasticsearch-powered fuzzy search across cases, entities, descriptions
- Filter by risk level and crime category

### Analytics Dashboard
- Live statistics: total cases, active investigations, critical alerts, flagged transaction value
- 30-day case timeline (line chart)
- Risk level distribution (doughnut chart)

### Data Ingestion Pipeline
- Drag-and-drop CSV upload for transactions and entities
- pandas ETL: cleaning, normalisation, type coercion, validation
- Auto-routing to ML scoring on every ingested transaction
- Pipeline health status (API, ML, Ingestion)

---

## Quick Start

### GitHub Codespaces (recommended)

1. Click **Code → Codespaces → New codespace** on this repo
2. Wait for the environment to build
3. In the terminal:

```bash
docker compose up sqlserver elasticsearch api ml-service ingestion -d
sleep 20
cd frontend && npm run dev
```

4. Open the forwarded port **3000** — the app loads with seeded sample data.

**Shortcut alias** (add to `~/.bashrc`):
```bash
alias ci-start="cd /workspaces/crimeintel-platform && docker compose up sqlserver elasticsearch api ml-service ingestion -d && sleep 20 && cd frontend && npm run dev"
```

### Local Development

**Prerequisites:** Docker Desktop, Git, Node.js 20+, .NET SDK 8, Python 3.11

```bash
git clone https://github.com/hanif-dev/crimeintel-platform
cd crimeintel-platform
cp .env.example .env
docker compose up sqlserver elasticsearch -d
sleep 15

# Terminal 1 — API
cd backend && dotnet run --project CrimeIntel.API

# Terminal 2 — ML Service
cd python-services/ml-service && pip install -r requirements.txt && uvicorn main:app --reload --port 8001

# Terminal 3 — Ingestion Service
cd python-services/ingestion && pip install -r requirements.txt && uvicorn main:app --reload --port 8000

# Terminal 4 — Frontend
cd frontend && npm install && npm run dev
```

---

## Service URLs

| Service | URL | Description |
|---|---|---|
| Frontend | http://localhost:3000 | Vue.js analyst workspace |
| API Swagger | http://localhost:5000/swagger | Interactive API docs |
| Ingestion Docs | http://localhost:8000/docs | FastAPI ETL docs |
| ML Docs | http://localhost:8001/docs | FastAPI ML scoring docs |
| Elasticsearch | http://localhost:9200 | Search engine |

---

## Project Structure

```
crimeintel-platform/
├── .devcontainer/
│   └── devcontainer.json          # GitHub Codespaces config
├── .github/
│   └── workflows/
│       └── ci.yml                 # GitHub Actions CI/CD pipeline
├── backend/
│   └── CrimeIntel.API/
│       ├── Controllers/           # REST API endpoints
│       ├── Data/                  # EF Core DbContext + seeder
│       ├── Models/                # Domain models (Case, Entity, Transaction)
│       └── Services/             # Business logic, Elasticsearch client
├── python-services/
│   ├── ingestion/                 # FastAPI ETL service
│   │   ├── main.py               # Endpoints + ETL pipeline
│   │   └── requirements.txt
│   └── ml-service/               # FastAPI ML scoring service
│       ├── main.py               # Isolation Forest model + API
│       └── requirements.txt
├── frontend/
│   └── src/
│       ├── api/                  # Axios API client
│       ├── components/           # NetworkGraph.vue (D3.js)
│       ├── router/               # Vue Router
│       ├── stores/               # Pinia state management
│       ├── types/                # TypeScript interfaces
│       └── views/                # Dashboard, Cases, Search, Ingest
├── data/
│   └── samples/                  # Sample CSV for testing ingestion
├── scripts/
│   └── codespace-init.sh         # Codespaces setup script
├── docker-compose.yml
├── .env.example
└── README.md
```

---

## Sample Data

The platform auto-seeds four intelligence cases on first run:

| Case | Category | Risk | Score |
|---|---|---|---|
| Shell Company Network — Baltic Region | Money Laundering | Critical | 94.2 |
| Ransomware Proceeds — Crypto Layering | Cybercrime | High | 87.5 |
| Dark Web Marketplace — Payment Processor | Organised Crime | High | 81.3 |
| Trade-Based Fraud — Import Overvaluation | Financial Fraud | Medium | 62.1 |

All data is entirely synthetic and does not represent real cases, individuals, or organisations.

---

## CI/CD Pipeline

GitHub Actions runs on every push to `main` and `develop`:

- **Backend** — `dotnet restore` → `dotnet build` → Docker build check
- **Python Ingestion** — `pip install` → `ruff` lint
- **Python ML** — `pip install` → unit test on scoring logic
- **Frontend** — `npm ci` → TypeScript type-check → `vite build`
- **Compose** — `docker compose config` syntax validation

---

## Skills Demonstrated

This project directly maps to the technical requirements for senior software engineering roles in international law enforcement and intelligence organisations:

| Skill | Implementation |
|---|---|
| C# / .NET 8 REST API | `CrimeIntel.API` — controllers, EF Core, Elasticsearch |
| Python ETL & tooling | `ingestion/` — FastAPI, pandas pipeline |
| Machine Learning | `ml-service/` — Isolation Forest, explainability |
| SQL Server | EF Core with migrations, relational schema |
| Elasticsearch | Full-text indexing, fuzzy search |
| Docker | 5-service compose stack |
| CI/CD | GitHub Actions — build, test, validate |
| Vue.js + TypeScript | SPA with Pinia, Vue Router, Tailwind |
| D3.js Link Analysis | Force-directed entity network graph |
| Agile/Scrum-ready | Feature branches, PR workflow, documented |

---

## Author

**Hanif** — [hanif-dev.github.io](https://hanif-dev.github.io)

---

## License

MIT — see [LICENSE](LICENSE)