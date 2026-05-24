using CrimeIntel.API.Data;
using CrimeIntel.API.Services;
using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Logging ───────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console());

    // ── Database ──────────────────────────────────────────────
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null)
        ));

    // ── Elasticsearch ─────────────────────────────────────────
    var esUri = builder.Configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";
    var esSettings = new ElasticsearchClientSettings(new Uri(esUri))
        .DefaultIndex("crimeintel");
    builder.Services.AddSingleton(new ElasticsearchClient(esSettings));

    // ── Application Services ──────────────────────────────────
    builder.Services.AddScoped<ICaseService, CaseService>();
    builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();
    builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

    // ── Controllers ───────────────────────────────────────────
    builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "CrimeIntel API",
            Version = "v1",
            Description = "Intelligence Analysis Platform — Fraud & Criminal Network Detection"
        });
    });

    // ── CORS ──────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    var app = builder.Build();

    // ── Middleware ────────────────────────────────────────────
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CrimeIntel v1"));
    app.UseCors();
    app.MapControllers();

    // ── DB Migration + Seed ───────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var es = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Applying database migrations...");
        db.Database.EnsureCreated();

        logger.LogInformation("Ensuring Elasticsearch index...");
        await es.EnsureIndexAsync();

        if (!db.Cases.Any())
        {
            logger.LogInformation("Seeding sample data...");
            await DbSeeder.SeedAsync(db, es);
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
