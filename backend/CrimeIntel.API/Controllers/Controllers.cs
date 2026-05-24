using CrimeIntel.API.Models;
using CrimeIntel.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrimeIntel.API.Controllers;

// ── Cases Controller ──────────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
public class CasesController(ICaseService caseService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? category = null)
    {
        var cases = await caseService.GetAllAsync(page, size, status, category);
        return Ok(cases);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await caseService.GetByIdAsync(id);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Case c)
    {
        var created = await caseService.CreateAsync(c);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Case updated)
    {
        var result = await caseService.UpdateAsync(id, updated);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await caseService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id}/notes")]
    public async Task<IActionResult> AddNote(int id, [FromBody] AddNoteRequest req)
    {
        var note = await caseService.AddNoteAsync(id, req.Content, req.Author);
        return Ok(note);
    }

    [HttpGet("{id}/network")]
    public async Task<IActionResult> GetNetwork(int id)
    {
        var graph = await caseService.GetNetworkGraphAsync(id);
        return Ok(graph);
    }
}

public record AddNoteRequest(string Content, string Author);

// ── Search Controller ─────────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
public class SearchController(IElasticsearchService esService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? riskLevel = null,
        [FromQuery] string? category = null)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query parameter 'q' is required" });

        var results = await esService.SearchAsync(q, riskLevel, category);
        return Ok(new { query = q, count = results.Count, results });
    }
}

// ── Analytics Controller ──────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() =>
        Ok(await analyticsService.GetDashboardStatsAsync());

    [HttpGet("risk-distribution")]
    public async Task<IActionResult> RiskDistribution() =>
        Ok(await analyticsService.GetRiskDistributionAsync());

    [HttpGet("timeline")]
    public async Task<IActionResult> Timeline([FromQuery] int days = 30) =>
        Ok(await analyticsService.GetTimelineAsync(days));
}

// ── Health Controller ─────────────────────────────────────────────────────────

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy", service = "CrimeIntel API", timestamp = DateTime.UtcNow });
}
