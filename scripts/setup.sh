#!/bin/bash
set -e

echo "🔧 Setting up CrimeIntel Platform..."

# Copy env if not exists
if [ ! -f /workspace/.env ]; then
  cp /workspace/.env.example /workspace/.env
  echo "✅ Created .env from example"
fi

# Install frontend dependencies
echo "📦 Installing frontend dependencies..."
cd /workspace/frontend && npm install

# Restore .NET packages
echo "📦 Restoring .NET packages..."
cd /workspace/backend && dotnet restore CrimeIntel.API/CrimeIntel.API.csproj

# Install Python deps for ingestion
echo "🐍 Installing Python dependencies (ingestion)..."
cd /workspace/python-services/ingestion && pip install -r requirements.txt

# Install Python deps for ML
echo "🐍 Installing Python dependencies (ml-service)..."
cd /workspace/python-services/ml-service && pip install -r requirements.txt

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ Setup complete!"
echo ""
echo "Start all services:  docker compose up --build"
echo "Frontend only:       cd frontend && npm run dev"
echo "API only:            cd backend && dotnet run --project CrimeIntel.API"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
