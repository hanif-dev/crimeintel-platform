namespace CrimeIntel.API.Models;

public enum RiskLevel { Low, Medium, High, Critical }
public enum CaseStatus { Open, Active, UnderReview, Closed, Escalated }
public enum CrimeCategory { FinancialFraud, Cybercrime, Trafficking, TerrorismFinancing, MoneyLaundering, OrganisedCrime }

public class Case
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CrimeCategory Category { get; set; }
    public CaseStatus Status { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public double RiskScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? AssignedAnalyst { get; set; }
    public string? CountryCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<Entity> Entities { get; set; } = [];
    public List<Transaction> Transactions { get; set; } = [];
    public List<CaseNote> Notes { get; set; } = [];
}

public class Entity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Person, Company, Account, IP, etc.
    public string? Identifier { get; set; }
    public string? Country { get; set; }
    public double? RiskScore { get; set; }
    public string? Metadata { get; set; } // JSON blob
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
    public List<EntityConnection> SourceConnections { get; set; } = [];
    public List<EntityConnection> TargetConnections { get; set; } = [];
}

public class EntityConnection
{
    public int Id { get; set; }
    public int SourceEntityId { get; set; }
    public int TargetEntityId { get; set; }
    public string RelationshipType { get; set; } = string.Empty; // controls, funds, communicates_with
    public double? Weight { get; set; }
    public string? Evidence { get; set; }
    public Entity SourceEntity { get; set; } = null!;
    public Entity TargetEntity { get; set; } = null!;
}

public class Transaction
{
    public int Id { get; set; }
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? FromAccount { get; set; }
    public string? ToAccount { get; set; }
    public string? FromCountry { get; set; }
    public string? ToCountry { get; set; }
    public double FraudScore { get; set; }
    public bool IsFlagged { get; set; }
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
}

public class CaseNote
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CaseId { get; set; }
    public Case Case { get; set; } = null!;
}

// Elasticsearch document
public class CaseDocument
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public double RiskScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CountryCode { get; set; }
    public List<string> EntityNames { get; set; } = [];
}
