using System.Diagnostics.Metrics;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Caching.Distributed;
using OpenAI.Chat;

namespace WhatCar.Api.Services;

public class LlmClient
{
    private readonly AzureOpenAIClient _azureClient;
    private readonly string _deploymentName;
    private readonly ILogger<LlmClient> _logger;
    private readonly IDistributedCache _cache;

    // Metrics
    private static readonly Meter Meter = new("WhatCar.Api.LlmClient");
    private static readonly Histogram<long> RequestDurationHistogram = Meter.CreateHistogram<long>("llm_request_duration_ms", "ms", "LLM request duration in milliseconds");
    private static readonly Counter<long> TokensPromptCounter = Meter.CreateCounter<long>("llm_tokens_prompt", "tokens", "Total prompt tokens sent to LLM");
    private static readonly Counter<long> TokensCompletionCounter = Meter.CreateCounter<long>("llm_tokens_completion", "tokens", "Total completion tokens received from LLM");
    private static readonly Counter<long> TokensTotalCounter = Meter.CreateCounter<long>("llm_tokens_total", "tokens", "Total tokens used (prompt + completion)");
    private static long _cacheHits = 0;
    private static long _cacheMisses = 0;
    private static readonly Counter<long> CacheHitsCounter = Meter.CreateCounter<long>("llm_cache_hits", "hits", "Number of cache hits");
    private static readonly Counter<long> CacheMissesCounter = Meter.CreateCounter<long>("llm_cache_misses", "misses", "Number of cache misses");
    private static readonly ObservableGauge<double> CacheHitRateGauge = Meter.CreateObservableGauge(
        "llm_cache_hit_rate",
        () =>
        {
            var total = _cacheHits + _cacheMisses;
            return total > 0 ? (double)_cacheHits / total : 0.0;
        },
        "ratio",
        "Cache hit rate (hits / total requests)");
    private static readonly Counter<long> InvalidResponseCounter = Meter.CreateCounter<long>("llm_invalid_response", "errors", "Number of invalid/unparseable responses (potential hallucinations)");
    private static readonly Counter<long> ErrorCounter = Meter.CreateCounter<long>("llm_errors", "errors", "Number of LLM request errors");

    public LlmClient(AzureOpenAIClient azureClient, string deploymentName, ILogger<LlmClient> logger, IDistributedCache cache)
    {
        _azureClient = azureClient;
        _deploymentName = deploymentName;
        _logger = logger;
        _cache = cache;
    }

    public async Task<string> GenerateODataQueryAsync(string systemPrompt, string userPrompt, string schemaSummary, CancellationToken ct = default)
    {
        using var activity = System.Diagnostics.Activity.Current ?? new System.Diagnostics.Activity("LlmClient.GenerateODataQueryAsync").Start();
        _logger.LogInformation("Generating OData query with system prompt: {SystemPrompt} and user prompt: {UserPrompt}", systemPrompt, userPrompt);

        // Create a cache key based on systemPrompt and userPrompt
        var cacheKey = $"llm:{_deploymentName}:{ComputeHash(systemPrompt + userPrompt + schemaSummary)}";
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            _logger.LogInformation("Cache hit for key {CacheKey}", cacheKey);
            activity?.SetTag("llm.cache", "hit");
            Interlocked.Increment(ref _cacheHits);
            CacheHitsCounter.Add(1);
            return cached;
        }

        Interlocked.Increment(ref _cacheMisses);
        CacheMissesCounter.Add(1);
        var chatClient = _azureClient.GetChatClient(_deploymentName);
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };
        var requestOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = 256,
            Temperature = 0.2f,
            TopP = 1.0f,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await chatClient.CompleteChatAsync(messages, requestOptions, ct);
            sw.Stop();
            RequestDurationHistogram.Record(sw.ElapsedMilliseconds);

            var result = response.Value.Content[0].Text;

            // Track token usage
            var usage = response.Value.Usage;
            TokensPromptCounter.Add(usage.InputTokenCount);
            TokensCompletionCounter.Add(usage.OutputTokenCount);
            TokensTotalCounter.Add(usage.TotalTokenCount);

            _logger.LogInformation("LLM response: {Result} (Tokens: {PromptTokens} prompt + {CompletionTokens} completion = {TotalTokens} total, Duration: {DurationMs}ms)",
                result, usage.InputTokenCount, usage.OutputTokenCount, usage.TotalTokenCount, sw.ElapsedMilliseconds);

            activity?.SetTag("llm.result", result);
            activity?.SetTag("llm.tokens.prompt", usage.InputTokenCount);
            activity?.SetTag("llm.tokens.completion", usage.OutputTokenCount);
            activity?.SetTag("llm.tokens.total", usage.TotalTokenCount);
            activity?.SetTag("llm.duration_ms", sw.ElapsedMilliseconds);

            // Cache the result for future requests
            await _cache.SetStringAsync(cacheKey, result, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            }, ct);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            RequestDurationHistogram.Record(sw.ElapsedMilliseconds);
            ErrorCounter.Add(1);
            _logger.LogError(ex, "Error generating OData query via LLM");
            activity?.SetTag("llm.error", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Records an invalid response from the LLM (e.g., unparseable JSON, missing required fields).
    /// Used to track potential hallucinations or model issues.
    /// </summary>
    public static void RecordInvalidResponse()
    {
        InvalidResponseCounter.Add(1);
    }

    private static string ComputeHash(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}