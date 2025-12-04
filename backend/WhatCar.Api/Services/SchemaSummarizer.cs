using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.OData.Edm;
using WhatCar.ODataCore.Data;

namespace WhatCar.Api.Services;

public class SchemaSummarizer
{
    private readonly VehicleSalesDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly Microsoft.Extensions.Logging.ILogger<SchemaSummarizer> _logger;

    // Metrics
    private static readonly Meter Meter = new("WhatCar.Api.SchemaSummarizer");
    private static readonly Histogram<long> QueryDurationHistogram = Meter.CreateHistogram<long>("schema_summary_query_duration_ms", "ms", "Schema summary SQL query duration in milliseconds");
    private static long _cacheHits = 0;
    private static long _cacheMisses = 0;
    private static readonly Counter<long> CacheHitsCounter = Meter.CreateCounter<long>("schema_summary_cache_hits", "hits", "Number of cache hits");
    private static readonly Counter<long> CacheMissesCounter = Meter.CreateCounter<long>("schema_summary_cache_misses", "misses", "Number of cache misses");
    private static readonly ObservableGauge<double> CacheHitRateGauge = Meter.CreateObservableGauge(
        "schema_summary_cache_hit_rate",
        () =>
        {
            var total = _cacheHits + _cacheMisses;
            return total > 0 ? (double)_cacheHits / total : 0.0;
        },
        "ratio",
        "Cache hit rate (hits / total requests)");
    private static readonly Counter<long> ErrorCounter = Meter.CreateCounter<long>("schema_summary_errors", "errors", "Number of schema summary query errors");

    public SchemaSummarizer(VehicleSalesDbContext db, IDistributedCache cache, Microsoft.Extensions.Logging.ILogger<SchemaSummarizer> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GenerateSummaryAsync()
    {
        using var activity = System.Diagnostics.Activity.Current ?? new System.Diagnostics.Activity("SchemaSummarizer.GenerateSummaryAsync").Start();
        var cacheKey = "SchemaSummary";
        _logger.LogInformation("Generating schema summary. Checking cache for key: {CacheKey}", cacheKey);
        var cached = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            _logger.LogInformation("Cache hit for schema summary.");
            activity?.SetTag("schema.cache", "hit");
            Interlocked.Increment(ref _cacheHits);
            CacheHitsCounter.Add(1);
            return cached;
        }

        _logger.LogInformation("Cache miss. Fetching schema summary from SQL function.");
        activity?.SetTag("schema.cache", "miss");
        Interlocked.Increment(ref _cacheMisses);
        CacheMissesCounter.Add(1);

        // Diagnostic logging: log connection string and server info
        var conn = _db.Database.GetDbConnection();
        _logger.LogInformation("Using connection string: {ConnectionString}", conn.ConnectionString);
        _logger.LogInformation("Database: {Database}, DataSource: {DataSource}", conn.Database, conn.DataSource);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Starting schema summary SQL query at {StartTime}", DateTime.UtcNow);
        string summary = null;
        try
        {
            summary = await _db.Database.SqlQueryRaw<string>("SELECT dbo.GetSchemaSummary() AS [Value]").FirstAsync();
            sw.Stop();
            QueryDurationHistogram.Record(sw.ElapsedMilliseconds);
            _logger.LogInformation("Schema summary SQL query completed at {EndTime} (Elapsed: {ElapsedMs} ms)", DateTime.UtcNow, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            QueryDurationHistogram.Record(sw.ElapsedMilliseconds);
            ErrorCounter.Add(1);
            _logger.LogError(ex, "Schema summary SQL query failed after {ElapsedMs} ms", sw.ElapsedMilliseconds);
            throw;
        }

        summary = summary.Replace("\r\n", "\n").Replace("\r", "\n");
        await _cache.SetStringAsync(cacheKey, summary, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
        });
        _logger.LogInformation("Schema summary fetched and cached.");
        return summary;
    }
}