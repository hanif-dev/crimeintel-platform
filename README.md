# CrimeIntel Platform

> Intelligence Analysis Platform for Fraud & Criminal Network Detection

[![CI](https://github.com/YOUR_USERNAME/crimeintel-platform/actions/workflows/ci.yml/badge.svg)](https://github.com/YOUR_USERNAME/crimeintel-platform/actions/workflows/ci.yml)

A full-stack intelligence analysis platform demonstrating production-grade software development across multiple technology stacks. Built to mirror real-world law enforcement intelligence systems.

---

## Architecture

```
┌──────────────────────────────────────────────────────────┐
│                   CrimeIntel Platform                    │
│                                                          │
│  Vue.js 3 + TypeScript (Frontend)   :3000               │
│  ├─ D3.js Network Graph (Link Analysis)                  │
│  ├─ Chart.js Dashboard (Analytics)                       │
│  └─ Elasticsearch-powered Search                         │
│                                                          │
│  .NET 8 REST API (Backend)          :5000               │
│  ├─ Entity Framework Core + SQL Server                   │
│  ├─ Elasticsearch NEST client                            │
│  └─ Microservice-ready architecture                      │
│                                                          │
│  FastAPI Ingestion Service (Python) :8000               │
│  ├─ CSV/JSON ETL pipeline                                │
│  └─ pandas data cleaning + validation                    │
│                                                          │
│  FastAPI ML Service (Python)        :8001               │
│  ├─ Isolation Forest anomaly detection                   │
│  └─ Feature explainability                               │
│                                                          │
│  Infrastructure (Docker)                                 │
│  ├─ SQL Server 2022                 :1433               │
│  └─ Elasticsearch 8.x              :9200               │
└──────────────────────────────────────────────────────────┘
```

## Tech Stack

| Layer | Technology |
|---|---|
| **Frontend** | Vue 3, TypeScript, Pinia, Vue Router, Tailwind CSS |
| **Visualisation** | D3.js (network graph), Chart.js (analytics) |
| **Backend API** | .NET 8, C#, Entity Framework Core, Swagger |
| **Search** | Elasticsearch 8.x |
| **Database** | Microsoft SQL Server 2022 |
| **ETL** | Python, FastAPI, pandas |
| **ML** | scikit-learn (Isolation Forest), numpy |
| **DevOps** | Docker, docker-compose, GitHub Actions CI/CD |

## Quick Start (GitHub Codespaces)

1. **Open in Codespace** — click the green Code button → Codespaces → New
2. **Copy env file:**
   ```bash
   cp .env.example .env
   ```
3. **Start all services:**
   ```bash
   docker compose up --build
   ```
4. Visit the forwarded port **3000** for the frontend

The app auto-seeds sample intelligence cases on first run.

## Quick Start (Local)

**Prerequisites:** Docker Desktop, Git

```bash
git clone https://github.com/YOUR_USERNAME/crimeintel-platform
cd crimeintel-platform
cp .env.example .env
docker compose up --build
```

| Service | URL |
|---|---|
| Frontend | http://localhost:3000 |
| API + Swagger | http://localhost:5000/swagger |
| Ingestion API | http://localhost:8000/docs |
| ML API | http://localhost:8001/docs |
| Elasticsearch | http://localhost:9200 |

## Key Features

### 🔍 Intelligence Case Management
- Full CRUD for cases with risk scoring, status tracking, and analyst assignment
- Category classification: Financial Fraud, Cybercrime, Money Laundering, Terrorism Financing, etc.

### ⬡ Link Analysis (Network Graph)
- D3.js force-directed graph visualising entity relationships
- Supports Person → Company → Account → IP relationship types
- Interactive: drag nodes, zoom, hover for details

### 🤖 ML Fraud Scoring
- Isolation Forest anomaly detection trained on synthetic transaction data
- 7-feature model: amount, cross-border flag, high-risk country flags, timing, currency, round amounts
- Feature importance explainability per transaction
- Batch scoring via REST API

### 📊 Analytics Dashboard
- Real-time statistics: active cases, critical flags, flagged transaction value
- Timeline chart (last 30 days)
- Risk distribution doughnut chart

### 🔎 Full-Text Search
- Elasticsearch-powered fuzzy search across cases, entities, descriptions
- Filter by risk level and crime category

### ⊞ Data Ingestion Pipeline
- CSV upload with drag-and-drop
- pandas-based ETL: cleaning, normalisation, validation
- Auto-routing to ML scoring service on ingest

## Development

```bash
# Backend only
cd backend && dotnet run --project CrimeIntel.API

# Frontend only
cd frontend && npm install && npm run dev

# ML service only
cd python-services/ml-service && uvicorn main:app --reload --port 8001

# Ingestion only
cd python-services/ingestion && uvicorn main:app --reload --port 8000
```

## Project Structure

```
crimeintel-platform/
├── .devcontainer/          # GitHub Codespaces configuration
├── .github/workflows/      # CI/CD (GitHub Actions)
├── backend/
│   └── CrimeIntel.API/
│       ├── Controllers/    # REST endpoints
│       ├── Services/       # Business logic + Elasticsearch
│       ├── Models/         # Domain models (Case, Entity, Transaction)
│       └── Data/           # EF Core DbContext + seeder
├── python-services/
│   ├── ingestion/          # FastAPI ETL service
│   └── ml-service/         # FastAPI ML scoring service
├── frontend/
│   └── src/
│       ├── views/          # Dashboard, Cases, Search, Ingest
│       ├── components/     # NetworkGraph (D3), shared UI
│       ├── stores/         # Pinia state management
│       ├── api/            # Axios API client
│       └── types/          # TypeScript interfaces
├── docker-compose.yml
└── .env.example
```

## License

MIT — built for portfolio demonstration purposes.
Sample data is entirely synthetic and does not represent real cases or individuals.
