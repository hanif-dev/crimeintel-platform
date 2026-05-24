"""
CrimeIntel — Data Ingestion & ETL Service
Handles CSV/JSON uploads, data cleaning, and loading into SQL Server + Elasticsearch.
"""
import io
import json
import os
from datetime import datetime
from typing import Optional

import pandas as pd
import pyodbc
import requests
from elasticsearch import Elasticsearch
from fastapi import FastAPI, File, HTTPException, Query, UploadFile
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel

app = FastAPI(
    title="CrimeIntel Ingestion Service",
    description="ETL pipeline for ingesting financial and operational data",
    version="1.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# ── Config ────────────────────────────────────────────────────────────────────

SQL_SERVER   = os.getenv("SQL_SERVER", "localhost")
SQL_USER     = os.getenv("SQL_USER", "sa")
SQL_PASSWORD = os.getenv("SQL_PASSWORD", "CrimeIntel@2024!")
SQL_DATABASE = os.getenv("SQL_DATABASE", "CrimeIntel")
ES_URL       = os.getenv("ELASTICSEARCH_URL", "http://localhost:9200")
ML_URL       = os.getenv("ML_URL", "http://ml-service:8001")

es = Elasticsearch([ES_URL])


def get_db_conn():
    conn_str = (
        f"DRIVER={{ODBC Driver 18 for SQL Server}};"
        f"SERVER={SQL_SERVER},1433;"
        f"DATABASE={SQL_DATABASE};"
        f"UID={SQL_USER};"
        f"PWD={SQL_PASSWORD};"
        "TrustServerCertificate=yes;"
    )
    return pyodbc.connect(conn_str)


# ── Models ────────────────────────────────────────────────────────────────────

class TransactionIngest(BaseModel):
    transaction_id: str
    timestamp: datetime
    amount: float
    currency: str = "EUR"
    from_account: Optional[str] = None
    to_account: Optional[str] = None
    from_country: Optional[str] = None
    to_country: Optional[str] = None
    case_id: int

class IngestResult(BaseModel):
    processed: int
    flagged: int
    failed: int
    errors: list[str]


# ── ETL Functions ─────────────────────────────────────────────────────────────

def clean_transactions_df(df: pd.DataFrame) -> pd.DataFrame:
    """Normalize and validate a transactions DataFrame."""
    required = {"amount", "timestamp", "from_account", "to_account"}
    missing = required - set(df.columns)
    if missing:
        raise ValueError(f"Missing columns: {missing}")

    df = df.copy()
    df["amount"] = pd.to_numeric(df["amount"], errors="coerce")
    df["timestamp"] = pd.to_datetime(df["timestamp"], errors="coerce")
    df = df.dropna(subset=["amount", "timestamp"])
    df["amount"] = df["amount"].abs()
    df["currency"] = df.get("currency", "EUR").fillna("EUR").str.upper()
    df["from_country"] = df.get("from_country", pd.NA).str.upper() if "from_country" in df.columns else pd.NA
    df["to_country"] = df.get("to_country", pd.NA).str.upper() if "to_country" in df.columns else pd.NA

    return df


def score_transactions(transactions: list[dict]) -> list[float]:
    """Send transactions to ML service for fraud scoring."""
    try:
        resp = requests.post(f"{ML_URL}/score/batch", json={"transactions": transactions}, timeout=10)
        resp.raise_for_status()
        return resp.json()["scores"]
    except Exception:
        # fallback: simple rule-based heuristic
        scores = []
        for t in transactions:
            score = 0.0
            if t.get("amount", 0) > 100_000:
                score += 30
            if t.get("from_country") in {"RU", "KP", "IR"} or t.get("to_country") in {"RU", "KP", "IR"}:
                score += 40
            if t.get("from_country") != t.get("to_country"):
                score += 15
            scores.append(min(score, 100.0))
        return scores


# ── Endpoints ─────────────────────────────────────────────────────────────────

@app.get("/health")
def health():
    return {"status": "ok", "service": "ingestion", "timestamp": datetime.utcnow().isoformat()}


@app.post("/ingest/transactions/json", response_model=IngestResult)
async def ingest_json(transactions: list[TransactionIngest], case_id: int = Query(...)):
    """Ingest a JSON list of transactions for a given case."""
    raw = [t.model_dump() for t in transactions]
    df = pd.DataFrame(raw)
    return await _process_transactions(df, case_id)


@app.post("/ingest/transactions/csv", response_model=IngestResult)
async def ingest_csv(file: UploadFile = File(...), case_id: int = Query(...)):
    """Upload a CSV file of transactions."""
    if not file.filename.endswith(".csv"):
        raise HTTPException(400, "File must be a CSV")
    content = await file.read()
    df = pd.read_csv(io.StringIO(content.decode("utf-8")))
    return await _process_transactions(df, case_id)


async def _process_transactions(df: pd.DataFrame, case_id: int) -> IngestResult:
    errors = []
    processed = flagged = failed = 0

    try:
        df = clean_transactions_df(df)
    except ValueError as e:
        raise HTTPException(422, str(e))

    records = df.to_dict(orient="records")
    scores = score_transactions(records)

    try:
        conn = get_db_conn()
        cursor = conn.cursor()
    except Exception as e:
        raise HTTPException(503, f"Database connection failed: {e}")

    for record, score in zip(records, scores):
        try:
            is_flagged = score >= 70
            cursor.execute(
                """
                INSERT INTO Transactions
                  (TransactionId, Timestamp, Amount, Currency,
                   FromAccount, ToAccount, FromCountry, ToCountry,
                   FraudScore, IsFlagged, CaseId)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """,
                str(record.get("transaction_id", "")),
                record["timestamp"],
                float(record["amount"]),
                record.get("currency", "EUR"),
                record.get("from_account"),
                record.get("to_account"),
                record.get("from_country"),
                record.get("to_country"),
                float(score),
                is_flagged,
                case_id,
            )
            processed += 1
            if is_flagged:
                flagged += 1
        except Exception as e:
            failed += 1
            errors.append(str(e))

    conn.commit()
    conn.close()

    return IngestResult(processed=processed, flagged=flagged, failed=failed, errors=errors)


@app.post("/ingest/entities/csv")
async def ingest_entities(file: UploadFile = File(...), case_id: int = Query(...)):
    """Upload a CSV of entities (persons, companies, accounts) for a case."""
    content = await file.read()
    df = pd.read_csv(io.StringIO(content.decode("utf-8")))

    required = {"name", "type"}
    if not required.issubset(df.columns):
        raise HTTPException(422, f"CSV must contain columns: {required}")

    df["name"] = df["name"].str.strip()
    df["type"] = df["type"].str.strip()
    df["risk_score"] = pd.to_numeric(df.get("risk_score", 50), errors="coerce").fillna(50)

    try:
        conn = get_db_conn()
        cursor = conn.cursor()
        for _, row in df.iterrows():
            cursor.execute(
                "INSERT INTO Entities (Name, Type, Country, RiskScore, CaseId) VALUES (?, ?, ?, ?, ?)",
                row["name"],
                row["type"],
                row.get("country", None),
                float(row["risk_score"]),
                case_id,
            )
        conn.commit()
        conn.close()
    except Exception as e:
        raise HTTPException(503, str(e))

    return {"inserted": len(df)}


@app.get("/pipeline/status")
def pipeline_status():
    """Check connectivity to downstream services."""
    db_ok = es_ok = ml_ok = False
    try:
        get_db_conn().close()
        db_ok = True
    except Exception:
        pass
    try:
        es_ok = es.ping()
    except Exception:
        pass
    try:
        ml_ok = requests.get(f"{ML_URL}/health", timeout=3).ok
    except Exception:
        pass

    return {"database": db_ok, "elasticsearch": es_ok, "ml_service": ml_ok}
