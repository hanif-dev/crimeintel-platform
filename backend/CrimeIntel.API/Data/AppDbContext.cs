using CrimeIntel.API.Models;
using CrimeIntel.API.Services;
using Microsoft.EntityFrameworkCore;

namespace CrimeIntel.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<EntityConnection> EntityConnections => Set<EntityConnection>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<CaseNote> CaseNotes => Set<CaseNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Case>(e =>
        {
            e.HasIndex(c => c.CaseNumber).IsUnique();
            e.Property(c => c.RiskScore).HasColumnType("float");
        });

        modelBuilder.Entity<EntityConnection>(e =>
        {
            e.HasOne(ec => ec.SourceEntity)
             .WithMany(en => en.SourceConnections)
             .HasForeignKey(ec => ec.SourceEntityId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ec => ec.TargetEntity)
             .WithMany(en => en.TargetConnections)
             .HasForeignKey(ec => ec.TargetEntityId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.Property(t => t.FraudScore).HasColumnType("float");
        });
    }
}

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IElasticsearchService es)
    {
        var cases = new List<Case>
        {
            new()
            {
                CaseNumber = "CI-2024-0001",
                Title = "Shell Company Network — Baltic Region",
                Description = "Complex web of shell companies used to launder proceeds from cybercrime. Funds routed through 5 jurisdictions via correspondent banking.",
                Category = CrimeCategory.MoneyLaundering,
                Status = CaseStatus.Active,
                RiskLevel = RiskLevel.Critical,
                RiskScore = 94.2,
                CountryCode = "LV",
                Latitude = 56.946, Longitude = 24.105,
                AssignedAnalyst = "analyst.01",
                Entities =
                [
                    new() { Name = "Baltic Holdings Ltd", Type = "Company", Country = "LV", RiskScore = 91 },
                    new() { Name = "Nordic Ventures BV", Type = "Company", Country = "NL", RiskScore = 85 },
                    new() { Name = "Subject Alpha", Type = "Person", Country = "RU", RiskScore = 97 },
                ],
                Transactions =
                [
                    new() { Timestamp = DateTime.UtcNow.AddDays(-30), Amount = 480000, Currency = "EUR", FromAccount = "LV21PARX0000000000001", ToAccount = "NL20INGB0001234567", FromCountry = "LV", ToCountry = "NL", FraudScore = 89, IsFlagged = true },
                    new() { Timestamp = DateTime.UtcNow.AddDays(-22), Amount = 320000, Currency = "EUR", FromAccount = "NL20INGB0001234567", ToAccount = "CY21002001950000357001150003", FromCountry = "NL", ToCountry = "CY", FraudScore = 92, IsFlagged = true },
                ]
            },
            new()
            {
                CaseNumber = "CI-2024-0002",
                Title = "Ransomware Proceeds — Crypto Layering",
                Description = "Cryptocurrency-based layering scheme following ransomware attack on EU healthcare infrastructure. Funds split across 47 wallets.",
                Category = CrimeCategory.Cybercrime,
                Status = CaseStatus.Active,
                RiskLevel = RiskLevel.High,
                RiskScore = 87.5,
                CountryCode = "DE",
                Latitude = 52.520, Longitude = 13.405,
                AssignedAnalyst = "analyst.02",
                Entities =
                [
                    new() { Name = "Wallet 0x1a2b...9f0e", Type = "CryptoWallet", RiskScore = 95 },
                    new() { Name = "Subject Beta", Type = "Person", Country = "UA", RiskScore = 88 },
                ]
            },
            new()
            {
                CaseNumber = "CI-2024-0003",
                Title = "Trade-Based Fraud — Import Overvaluation",
                Description = "Systematic overvaluation of imported electronics to move funds across borders. Estimated exposure: €2.3M.",
                Category = CrimeCategory.FinancialFraud,
                Status = CaseStatus.UnderReview,
                RiskLevel = RiskLevel.Medium,
                RiskScore = 62.1,
                CountryCode = "PL",
                Latitude = 52.237, Longitude = 21.017,
                AssignedAnalyst = "analyst.01",
                Entities =
                [
                    new() { Name = "EastTech Trading Sp. z o.o.", Type = "Company", Country = "PL", RiskScore = 71 },
                    new() { Name = "Subject Gamma", Type = "Person", Country = "PL", RiskScore = 65 },
                ]
            },
            new()
            {
                CaseNumber = "CI-2024-0004",
                Title = "Dark Web Marketplace — Payment Processor",
                Description = "Payment processing node for dark web narcotics marketplace, accepting crypto and hawala transfers.",
                Category = CrimeCategory.OrganisedCrime,
                Status = CaseStatus.Open,
                RiskLevel = RiskLevel.High,
                RiskScore = 81.3,
                CountryCode = "ES",
                Latitude = 40.416, Longitude = -3.703,
                AssignedAnalyst = "analyst.03",
                Entities =
                [
                    new() { Name = "Subject Delta", Type = "Person", Country = "ES", RiskScore = 82 },
                    new() { Name = "192.168.44.201", Type = "IPAddress", RiskScore = 78 },
                ]
            },
        };

        db.Cases.AddRange(cases);
        await db.SaveChangesAsync();

        // Add connections for case 1
        var c1Entities = cases[0].Entities.ToList();
        db.EntityConnections.AddRange(
            new EntityConnection { SourceEntityId = c1Entities[2].Id, TargetEntityId = c1Entities[0].Id, RelationshipType = "controls", Weight = 0.95, Evidence = "Beneficial ownership registry" },
            new EntityConnection { SourceEntityId = c1Entities[0].Id, TargetEntityId = c1Entities[1].Id, RelationshipType = "funds", Weight = 0.88, Evidence = "Bank transfer records" }
        );
        await db.SaveChangesAsync();

        // Index in Elasticsearch
        foreach (var c in cases)
        {
            await es.IndexCaseAsync(c);
        }
    }
}
