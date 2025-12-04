using System.Diagnostics.Metrics;
using System.Text.Json;

namespace WhatCar.Api.Services;

/// <summary>
/// Service for executing OData queries against the whatcar-odata service.
/// </summary>
public class ODataExecutor
{
    private readonly HttpClient _odataClient;
    private readonly ILogger<ODataExecutor> _logger;
    private static readonly Meter Meter = new("WhatCar.Api.ODataExecutor");
    private static readonly Histogram<long> QueryDurationHistogram = Meter.CreateHistogram<long>("odata_query_duration_ms", "ms", "OData query duration in milliseconds");

    /// <summary>
    /// Initializes a new instance of the <see cref="ODataExecutor"/> class.
    /// </summary>
    /// <param name="odataClient">The HTTP client configured for whatcar-odata.</param>
    /// <param name="logger">The logger instance.</param>
    public ODataExecutor(HttpClient odataClient, ILogger<ODataExecutor> logger)
    {
        _odataClient = odataClient;
        _logger = logger;
    }

    public async Task<JsonDocument> ExecuteAsync(string odataQuery, CancellationToken ct = default)
    {
        var url = $"/odata/{odataQuery}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("ODataExecutor: Sending GET {Url}", url);
            var res = await _odataClient.GetAsync(url, ct);
            _logger.LogInformation("ODataExecutor: Response status code {StatusCode}", res.StatusCode);
            res.EnsureSuccessStatusCode();

            sw.Stop();
            QueryDurationHistogram.Record(sw.ElapsedMilliseconds);

            var stream = await res.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // This is a timeout, not a user cancellation
            sw.Stop();
            QueryDurationHistogram.Record(sw.ElapsedMilliseconds);
            _logger.LogWarning(ex, "ODataExecutor: Query timed out after {ElapsedMs}ms: {Query}", sw.ElapsedMilliseconds, odataQuery);
            throw new TimeoutException($"OData query timed out after {sw.ElapsedMilliseconds}ms", ex);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // This is a timeout from the resilience pipeline
            sw.Stop();
            QueryDurationHistogram.Record(sw.ElapsedMilliseconds);
            _logger.LogWarning(ex, "ODataExecutor: Query was cancelled by resilience policy after {ElapsedMs}ms: {Query}", sw.ElapsedMilliseconds, odataQuery);
            throw new TimeoutException($"OData query was cancelled by timeout policy after {sw.ElapsedMilliseconds}ms", ex);
        }
        catch (Exception ex)
        {
            sw.Stop();
            QueryDurationHistogram.Record(sw.ElapsedMilliseconds);
            _logger.LogError(ex, "ODataExecutor: Error executing query {Query}", odataQuery);
            throw;
        }
    }

    /// <summary>
    /// Executes an OData query and returns the response stream directly for streaming to clients.
    /// </summary>
    /// <param name="odataQuery">The OData query string (e.g., "SalesData?filter=Year eq 2024").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response stream.</returns>
    public async Task<Stream> ExecuteStreamAsync(string odataQuery, CancellationToken ct = default)
    {
        var url = $"/odata/{odataQuery}";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("ODataExecutor: Sending GET (streaming) {Url}", url);
            var res = await _odataClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            _logger.LogInformation("ODataExecutor: Response status code {StatusCode}", res.StatusCode);
            res.EnsureSuccessStatusCode();

            sw.Stop();
            QueryDurationHistogram.Record(sw.ElapsedMilliseconds);

            return await res.Content.ReadAsStreamAsync(ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            QueryDurationHistogram.Record(sw.ElapsedMilliseconds);
            _logger.LogWarning(ex, "ODataExecutor: Streaming query timed out after {ElapsedMs}ms: {Query}", sw.ElapsedMilliseconds, odataQuery);
            throw new TimeoutException($"OData streaming query timed out after {sw.ElapsedMilliseconds}ms", ex);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            QueryDurationHistogram.Record(sw.ElapsedMilliseconds);
            _logger.LogWarning(ex, "ODataExecutor: Streaming query was cancelled by resilience policy after {ElapsedMs}ms: {Query}", sw.ElapsedMilliseconds, odataQuery);
            throw new TimeoutException($"OData streaming query was cancelled by timeout policy after {sw.ElapsedMilliseconds}ms", ex);
        }
        catch (Exception ex)
        {
            sw.Stop();
            QueryDurationHistogram.Record(sw.ElapsedMilliseconds);
            _logger.LogError(ex, "ODataExecutor: Error executing query (streaming) {Query}", odataQuery);
            throw;
        }
    }
}