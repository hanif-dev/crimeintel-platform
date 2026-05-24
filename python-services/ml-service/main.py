"""
CrimeIntel — ML Scoring Service
Anomaly detection and fraud scoring using Isolation Forest.
Provides explainability via feature contribution breakdown.
"""
import os
from datetime import datetime
from typing import Optional

import numpy as np
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from sklearn.ensemble import IsolationForest
from sklearn.preprocessing import StandardScaler

app = FastAPI(
    title="CrimeIntel ML Service",
    description="Fraud scoring and anomaly detection engine",
    version="1.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

# ── Model initialisation (trained on synthetic data at startup) ───────────────

RISK_COUNTRIES = {"RU", "KP", "IR", "BY", "SY", "MM", "CU", "VE"}
HIGH_RISK_CURRENCIES = {"RUB", "IRR"}

class ModelStore:
    model: Optional[IsolationForest] = None
    scaler: Optional[StandardScaler] = None

store = ModelStore()


def _extract_features(t: dict) -> list[float]:
    amount     = float(t.get("amount", 0))
    hour       = datetime.fromisoformat(str(t.get("timestamp", "2024-01-01"))).hour if t.get("timestamp") else 12
    cross_border = int(t.get("from_country", "") != t.get("to_country", ""))
    from_risk  = int(str(t.get("from_country", "")).upper() in RISK_COUNTRIES)
    to_risk    = int(str(t.get("to_country",   "")).upper() in RISK_COUNTRIES)
    odd_hour   = int(hour < 6 or hour > 22)
    high_risk_currency = int(str(t.get("currency", "")).upper() in HIGH_RISK_CURRENCIES)
    amount_log = float(np.log1p(amount))
    round_amount = int(amount % 1000 == 0 and amount > 0)
    return [amount_log, cross_border, from_risk, to_risk, odd_hour, high_risk_currency, round_amount]


def _train_model():
    """Train Isolation Forest on synthetic data representing known fraud patterns."""
    rng = np.random.default_rng(42)

    # Legitimate transactions (low risk)
    legit = np.column_stack([
        np.log1p(rng.uniform(100, 50_000, 800)),   # amount_log
        rng.integers(0, 2, 800),                    # cross_border
        np.zeros(800),                              # from_risk
        np.zeros(800),                              # to_risk
        np.zeros(800),                              # odd_hour
        np.zeros(800),                              # high_risk_currency
        np.zeros(800),                              # round_amount
    ])

    # Suspicious transactions (high risk)
    fraud = np.column_stack([
        np.log1p(rng.uniform(200_000, 2_000_000, 200)),
        np.ones(200),
        rng.integers(0, 2, 200),
        rng.integers(0, 2, 200),
        rng.integers(0, 2, 200),
        rng.integers(0, 2, 200),
        np.ones(200),
    ])

    X = np.vstack([legit, fraud])

    scaler = StandardScaler()
    X_scaled = scaler.fit_transform(X)

    model = IsolationForest(
        n_estimators=200,
        contamination=0.2,
        random_state=42,
        n_jobs=-1,
    )
    model.fit(X_scaled)

    store.model = model
    store.scaler = scaler
    return model, scaler


@app.on_event("startup")
async def startup():
    _train_model()


# ── Models ────────────────────────────────────────────────────────────────────

class Transaction(BaseModel):
    transaction_id: Optional[str] = None
    timestamp: Optional[str] = None
    amount: float
    currency: str = "EUR"
    from_account: Optional[str] = None
    to_account: Optional[str] = None
    from_country: Optional[str] = None
    to_country: Optional[str] = None

class BatchRequest(BaseModel):
    transactions: list[Transaction]

class ScoreResult(BaseModel):
    transaction_id: Optional[str]
    score: float
    risk_level: str
    is_anomaly: bool
    explanation: dict[str, float]


# ── Scoring logic ─────────────────────────────────────────────────────────────

def _score_one(t: dict) -> tuple[float, dict]:
    features = _extract_features(t)
    X = np.array([features])
    X_scaled = store.scaler.transform(X)

    raw_score = store.model.decision_function(X_scaled)[0]
    # Convert to 0-100 scale (lower decision fn score = more anomalous)
    normalised = float(np.clip(((-raw_score + 0.2) / 0.6) * 100, 0, 100))

    feature_names = [
        "amount", "cross_border", "from_high_risk_country",
        "to_high_risk_country", "odd_hour", "high_risk_currency", "round_amount"
    ]
    explanation = {name: round(float(val) * 20, 2) for name, val in zip(feature_names, features)}

    return round(normalised, 2), explanation


def _risk_label(score: float) -> str:
    if score >= 80: return "Critical"
    if score >= 60: return "High"
    if score >= 40: return "Medium"
    return "Low"


# ── Endpoints ─────────────────────────────────────────────────────────────────

@app.get("/health")
def health():
    return {
        "status": "ok",
        "service": "ml-service",
        "model": "IsolationForest",
        "trained": store.model is not None,
        "timestamp": datetime.utcnow().isoformat(),
    }


@app.post("/score", response_model=ScoreResult)
def score_single(transaction: Transaction):
    score, explanation = _score_one(transaction.model_dump())
    return ScoreResult(
        transaction_id=transaction.transaction_id,
        score=score,
        risk_level=_risk_label(score),
        is_anomaly=score >= 60,
        explanation=explanation,
    )


@app.post("/score/batch")
def score_batch(request: BatchRequest):
    results = []
    scores = []
    for t in request.transactions:
        score, explanation = _score_one(t.model_dump())
        scores.append(score)
        results.append(ScoreResult(
            transaction_id=t.transaction_id,
            score=score,
            risk_level=_risk_label(score),
            is_anomaly=score >= 60,
            explanation=explanation,
        ))
    return {"scores": scores, "results": results}


@app.post("/retrain")
def retrain():
    """Re-train the model (would use updated data in production)."""
    _train_model()
    return {"status": "retrained", "timestamp": datetime.utcnow().isoformat()}


@app.get("/model/info")
def model_info():
    if store.model is None:
        return {"error": "Model not trained"}
    return {
        "type": "IsolationForest",
        "estimators": store.model.n_estimators,
        "contamination": store.model.contamination,
        "features": [
            "amount_log", "cross_border", "from_high_risk_country",
            "to_high_risk_country", "odd_hour", "high_risk_currency", "round_amount",
        ],
    }
