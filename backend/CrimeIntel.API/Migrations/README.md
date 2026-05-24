# Migrations

Migrations are auto-applied on startup via `db.Database.Migrate()`.

To create a new migration (run from /backend):
```bash
dotnet ef migrations add <MigrationName> --project CrimeIntel.API
```

To apply manually:
```bash
dotnet ef database update --project CrimeIntel.API
```
