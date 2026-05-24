#!/bin/bash
set -e

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "   CrimeIntel Platform — Codespace Init"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Copy .env if not exists
if [ ! -f .env ]; then
  cp .env.example .env
  echo "✅ Created .env"
fi

# Install frontend deps
echo "📦 Installing frontend dependencies..."
cd frontend && npm install --silent && cd ..

# Restore .NET
echo "📦 Restoring .NET packages..."
dotnet restore backend/CrimeIntel.API/CrimeIntel.API.csproj --verbosity quiet

# Python ML service
echo "🐍 Installing ML service dependencies..."
pip install -r python-services/ml-service/requirements.txt --quiet

# Python Ingestion service
echo "🐍 Installing ingestion service dependencies..."
pip install fastapi uvicorn pandas requests elasticsearch python-multipart pydantic --quiet

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ Setup complete!"
echo ""
echo "  Next step — start everything:"
echo "  $ docker compose up --build"
echo ""
echo "  Or start services individually:"
echo "  $ cd frontend && npm run dev"
echo "  $ cd backend && dotnet run --project CrimeIntel.API"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
