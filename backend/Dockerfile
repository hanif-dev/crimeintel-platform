# ── Build stage ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CrimeIntel.API/CrimeIntel.API.csproj ./CrimeIntel.API/
RUN dotnet restore ./CrimeIntel.API/CrimeIntel.API.csproj

COPY . .
RUN dotnet publish ./CrimeIntel.API/CrimeIntel.API.csproj -c Release -o /app/publish

# ── Runtime stage ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CrimeIntel.API.dll"]
