using CrimeIntel.API.Data;
using CrimeIntel.API.Models;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.EntityFrameworkCore;

namespace CrimeIntel.API.Services;

// ── Interfaces ────────────────────────────────────────────────────────────────

public interface ICaseService
{
    Task<List<Case>> GetAllAsync(int page = 1, int size = 20, string? status = null, string? category = null);
    Task<Case?> GetByIdAsync(int id);
    Task<Case> CreateAsync(Case c);
    Task<Case?> UpdateAsync(int id, Case updated);
    Task<bool> DeleteAsync(int id);
    Task<CaseNote> AddNoteAsync(int caseId, string content, string author);
    Task<object> GetNetworkGraphAsync(int caseId);
}

public interface IElasticsearchService
{
    Task EnsureIndexAsync();
    Task IndexCaseAsync(Case c);
    Task<List<CaseDocument>> SearchAsync(string query, string? riskLevel = null, string? category = null);
}

public interface IAnalyticsService
{
    Task<object> GetDashboardStatsAsync();
    Task<object> GetRiskDistributionAsync();
    Task<object> GetTimelineAsync(int days = 30);
}

// ── CaseService ───────────────────────────────────────────────────────────────

public class CaseService(AppDbContext db, IElasticsearchService es) : ICaseService
{
    public async Task<List<Case>> GetAllAsync(int page = 1, int size = 20, string? status = null, string? category = null)
    {
        var query = db.Cases
            .Include(c => c.Entities)
            .Include(c => c.Transactions)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CaseStatus>(status, out var s))
            query = query.Where(c => c.Status == s);

        if (!string.IsNullOrEmpty(category) && Enum.TryParse<CrimeCategory>(category, out var cat))
            query = query.Where(c => c.Category == cat);

        return await query
            .OrderByDescending(c => c.RiskScore)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();
    }

    public async Task<Case?> GetByIdAsync(int id) =>
        await db.Cases
            .Include(c => c.Entities)
                .ThenInclude(e => e.SourceConnections).ThenInclude(ec => ec.TargetEntity)
            .Include(c => c.Transactions)
            .Include(c => c.Notes)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Case> CreateAsync(Case c)
    {
        c.CaseNumber = $"CI-{DateTime.UtcNow:yyyy}-{(db.Cases.Count() + 1):D4}";
        c.CreatedAt = c.UpdatedAt = DateTime.UtcNow;
        db.Cases.Add(c);
        await db.SaveChangesAsync();
        await es.IndexCaseAsync(c);
        return c;
    }

    public async Task<Case?> UpdateAsync(int id, Case updated)
    {
        var existing = await db.Cases.FindAsync(id);
        if (existing is null) return null;

        existing.Title = updated.Title;
        existing.Description = updated.Description;
        existing.Status = updated.Status;
        existing.RiskLevel = updated.RiskLevel;
        existing.RiskScore = updated.RiskScore;
        existing.AssignedAnalyst = updated.AssignedAnalyst;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await es.IndexCaseAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var c = await db.Cases.FindAsync(id);
        if (c is null) return false;
        db.Cases.Remove(c);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<CaseNote> AddNoteAsync(int caseId, string content, string author)
    {
        var note = new CaseNote { CaseId = caseId, Content = content, Author = author, CreatedAt = DateTime.UtcNow };
        db.CaseNotes.Add(note);
        await db.SaveChangesAsync();
        return note;
    }

    public async Task<object> GetNetworkGraphAsync(int caseId)
    {
        var entities = await db.Entities
            .Where(e => e.CaseId == caseId)
            .ToListAsync();

        var connections = await db.EntityConnections
            .Include(ec => ec.SourceEntity)
            .Include(ec => ec.TargetEntity)
            .Where(ec => ec.SourceEntity.CaseId == caseId)
            .ToListAsync();

        return new
        {
            nodes = entities.Select(e => new
            {
                id = e.Id.ToString(),
                label = e.Name,
                type = e.Type,
                risk = e.RiskScore,
                country = e.Country
            }),
            edges = connections.Select(ec => new
            {
                source = ec.SourceEntityId.ToString(),
                target = ec.TargetEntityId.ToString(),
                label = ec.RelationshipType,
                weight = ec.Weight,
                evidence = ec.Evidence
            })
        };
    }
}

// ── ElasticsearchService ──────────────────────────────────────────────────────

public class ElasticsearchService(ElasticsearchClient client) : IElasticsearchService
{
    private const string IndexName = "crimeintel-cases";

    public async Task EnsureIndexAsync()
    {
        var exists = await client.Indices.ExistsAsync(IndexName);
        if (!exists.Exists)
        {
            await client.Indices.CreateAsync(IndexName, c => c
                .Mappings(m => m
                    .Properties<CaseDocument>(p => p
                        .Text(t => t.Title)
                        .Text(t => t.Description)
                        .Keyword(k => k.Category)
                        .Keyword(k => k.Status)
                        .Keyword(k => k.RiskLevel)
                        .Date(d => d.CreatedAt)
                        .DoubleNumber(n => n.RiskScore)
                    )
                )
            );
        }
    }

    public async Task IndexCaseAsync(Case c)
    {
        var doc = new CaseDocument
        {
            Id = c.Id,
            CaseNumber = c.CaseNumber,
            Title = c.Title,
            Description = c.Description,
            Category = c.Category.ToString(),
            Status = c.Status.ToString(),
            RiskLevel = c.RiskLevel.ToString(),
            RiskScore = c.RiskScore,
            CreatedAt = c.CreatedAt,
            CountryCode = c.CountryCode,
            EntityNames = c.Entities.Select(e => e.Name).ToList()
        };
        await client.IndexAsync(doc, i => i.Index(IndexName).Id(c.Id.ToString()));
    }

    public async Task<List<CaseDocument>> SearchAsync(string query, string? riskLevel = null, string? category = null)
    {
        var response = await client.SearchAsync<CaseDocument>(s => s
            .Index(IndexName)
            .Query(q => q
                .Bool(b =>
                {
                    b.Must(m => m.MultiMatch(mm => mm
                        .Fields(new[] { "title^3", "description", "caseNumber^2", "entityNames" })
                        .Query(query)
                        .Fuzziness(new Fuzziness("AUTO"))
                    ));

                    if (!string.IsNullOrEmpty(riskLevel))
                        b.Filter(f => f.Term(t => t.Field(d => d.RiskLevel).Value(riskLevel)));

                    if (!string.IsNullOrEmpty(category))
                        b.Filter(f => f.Term(t => t.Field(d => d.Category).Value(category)));
                })
            )
            .Size(50)
        );

        return response.Documents.ToList();
    }
}

// ── AnalyticsService ──────────────────────────────────────────────────────────

public class AnalyticsService(AppDbContext db) : IAnalyticsService
{
    public async Task<object> GetDashboardStatsAsync()
    {
        var total = await db.Cases.CountAsync();
        var active = await db.Cases.CountAsync(c => c.Status == CaseStatus.Active);
        var critical = await db.Cases.CountAsync(c => c.RiskLevel == RiskLevel.Critical);
        var flagged = await db.Transactions.CountAsync(t => t.IsFlagged);
        var totalValue = await db.Transactions.Where(t => t.IsFlagged).SumAsync(t => t.Amount);

        return new { total, active, critical, flaggedTransactions = flagged, flaggedValue = totalValue };
    }

    public async Task<object> GetRiskDistributionAsync()
    {
        var dist = await db.Cases
            .GroupBy(c => c.RiskLevel)
            .Select(g => new { level = g.Key.ToString(), count = g.Count() })
            .ToListAsync();

        var catDist = await db.Cases
            .GroupBy(c => c.Category)
            .Select(g => new { category = g.Key.ToString(), count = g.Count() })
            .ToListAsync();

        return new { byRisk = dist, byCategory = catDist };
    }

    public async Task<object> GetTimelineAsync(int days = 30)
    {
        var from = DateTime.UtcNow.AddDays(-days);
        var timeline = await db.Cases
            .Where(c => c.CreatedAt >= from)
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), count = g.Count() })
            .OrderBy(x => x.date)
            .ToListAsync();

        return new { timeline };
    }
}
