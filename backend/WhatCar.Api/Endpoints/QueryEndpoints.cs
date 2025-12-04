using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using WhatCar.Api.Services;


namespace WhatCar.Api.Endpoints;

/// <summary>
/// Provides endpoints for natural language query orchestration and OData execution.
/// </summary>
public static class QueryEndpoints
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // allowedEntities should ideally be injected or static readonly, not derived from a summary string every time
    private static readonly HashSet<string> _allowedEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "SalesData",
        "Vehicles"
    };

    /// <summary>
    /// Maps query-related endpoints to the route builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The updated endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapQueryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/query").WithTags("Query");

        group.MapPost("", AskVehicleSales)
            .WithName("AskVehicleSales")
            .WithSummary("Accepts a natural language question, generates an OData query, executes it, and returns results.")
            .WithDescription("Processes a natural language question, generates an OData query using LLM, executes it, and returns the results.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/stream", AskVehicleSalesStream)
            .WithName("AskVehicleSalesStream")
            .WithSummary("Accepts a natural language question and streams the OData query results.")
            .WithDescription("Processes a natural language question, generates an OData query using LLM, executes it, and streams the results back to the client.")
            .Produces(StatusCodes.Status200OK, contentType: "application/json")
            .Produces(StatusCodes.Status400BadRequest);

        return group;
    }

    /// <summary>
    /// Accepts a natural language question, generates an OData query, executes it, and returns results.
    /// </summary>
    /// <param name="promptBuilder">Prompt builder service.</param>
    /// <param name="summarizer">Schema summarizer service.</param>
    /// <param name="llm">LLM client service.</param>
    /// <param name="executor">OData executor service.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="req">Query request model.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Query results or error.</returns>
    private static async Task<IResult> AskVehicleSales(
        PromptBuilder promptBuilder,
        SchemaSummarizer summarizer,
        LlmClient llm,
        ODataExecutor executor,
        ILoggerFactory loggerFactory,
        QueryRequest req,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("QueryOrchestration");

        var schema = await summarizer.GenerateSummaryAsync();
        var prompt = promptBuilder.Build(req.Question, schema);
        var llmOutput = await llm.GenerateODataQueryAsync(prompt, req.Question, schema, ct);

        if (string.IsNullOrWhiteSpace(llmOutput))
        {
            return Results.BadRequest(new { error = "The assistant did not produce a valid query." });
        }

        LlmQueryEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<LlmQueryEnvelope>(llmOutput, _jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LLM output was not valid JSON. Output: {Output}", llmOutput);
            return Results.BadRequest(new { error = "Invalid JSON format from assistant." });
        }

        if (envelope is null)
        {
            logger.LogWarning("LLM output could not be deserialized into an envelope. Output: {Output}", llmOutput);
            return Results.BadRequest(new { error = "The assistant did not provide a valid response." });
        }

        if (!string.IsNullOrWhiteSpace(envelope.Error))
        {
            logger.LogInformation("LLM returned error for question: {Question}. Error: {Error}", req.Question, envelope.Error);
            return Results.BadRequest(new { error = envelope.Error });
        }

        if (string.IsNullOrWhiteSpace(envelope.Query))
        {
            logger.LogWarning("LLM JSON envelope missing 'query'. Output: {Output}", llmOutput);
            return Results.BadRequest(new { error = "The assistant did not provide a query." });
        }

        if (!IsAllowedEntitySet(envelope.Query))
        {
            logger.LogWarning("Rejected LLM query targeting disallowed entity set. Query: {Query}", envelope.Query);
            return Results.BadRequest(new { error = "Query must target Vehicles or SalesData." });
        }

        try
        {
            logger.LogInformation("Executing OData query: {Query}", envelope.Query);

            using var dataDoc = await executor.ExecuteAsync(envelope.Query, ct);
            var dataElement = dataDoc.RootElement.Clone();

            var resultType = NormalizeResultType(envelope.ResultType, envelope.Query);

            // Validate that the data contains required fields for the result type
            var validation = ResultValidator.Validate(resultType, dataElement);
            if (!validation.IsValid)
            {
                logger.LogWarning("Data validation failed for resultType '{ResultType}': {Error}", resultType, validation.ErrorMessage);
                return Results.BadRequest(new
                {
                    error = $"Query succeeded but data is missing required fields. {validation.ErrorMessage}",
                    query = envelope.Query,
                    resultType
                });
            }

            logger.LogInformation("Success: {Question} -> {ResultType}", req.Question, resultType);

            return Results.Ok(new { resultType, data = dataElement });
        }
        catch (TimeoutException tex)
        {
            logger.LogWarning(tex, "Query timed out for question: {Question}", req.Question);
            return Results.Json(new
            {
                error = "The query took too long to execute. Try narrowing your question or adding filters.",
                details = tex.Message
            }, statusCode: StatusCodes.Status504GatewayTimeout);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.LogInformation("Request cancelled by client for question: {Question}", req.Question);
            return Results.StatusCode(499); // Client Closed Request
        }
        catch (HttpRequestException hex)
        {
            logger.LogError(hex, "HTTP error executing OData query for question: {Question}", req.Question);
            return Results.Json(new
            {
                error = "Unable to connect to the data service.",
                details = hex.Message
            }, statusCode: StatusCodes.Status502BadGateway);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OData Execution failed for question: {Question}", req.Question);
            return Results.BadRequest(new { error = "Failed to execute generated query.", details = ex.Message });
        }
    }

    /// <summary>
    /// Checks if the OData query targets an allowed entity set (Vehicles or SalesData).
    /// </summary>
    /// <param name="odataQuery">OData query string.</param>
    /// <returns>True if allowed, otherwise false.</returns>
    private static bool IsAllowedEntitySet(string odataQuery)
    {
        var span = odataQuery.AsSpan().Trim();
        int qIndex = span.IndexOf('?');
        var entitySet = qIndex > 0 ? span.Slice(0, qIndex) : span;

        return _allowedEntities.Contains(entitySet.ToString());
    }

    /// <summary>
    /// Normalizes the result type from the envelope or infers from query.
    /// </summary>
    /// <param name="resultType">Result type from envelope.</param>
    /// <param name="query">OData query string.</param>
    /// <returns>Normalized result type string.</returns>
    private static string NormalizeResultType(string? resultType, string query)
    {
        if (string.IsNullOrWhiteSpace(resultType))
        {
            return InferDefaultResultType(query);
        }

        var normalized = resultType.Trim().ToLowerInvariant();
        return normalized is "trend" or "ranking" or "comparison" or "table"
            ? normalized
            : InferDefaultResultType(query);
    }

    /// <summary>
    /// Infers the default result type for a query string.
    /// </summary>
    /// <param name="query">OData query string.</param>
    /// <returns>Result type string.</returns>
    private static string InferDefaultResultType(string query)
    {
        var q = query.ToLowerInvariant();
        if (q.Contains("year")) return "trend";
        if (q.Contains("$top") || q.Contains("unitssold")) return "ranking";
        if (q.Contains("fuel") || q.Contains("make") || q.Contains("model")) return "comparison";
        return "table";
    }

    /// <summary>
    /// Accepts a natural language question, generates an OData query, executes it, and streams results back to the client.
    /// </summary>
    private static async Task<IResult> AskVehicleSalesStream(
        PromptBuilder promptBuilder,
        SchemaSummarizer summarizer,
        LlmClient llm,
        ODataExecutor executor,
        ILoggerFactory loggerFactory,
        QueryRequest req,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("QueryOrchestration");

        var schema = await summarizer.GenerateSummaryAsync();
        var prompt = promptBuilder.Build(req.Question, schema);
        var llmOutput = await llm.GenerateODataQueryAsync(prompt, req.Question, schema, ct);

        if (string.IsNullOrWhiteSpace(llmOutput))
        {
            return Results.BadRequest(new { error = "The assistant did not produce a valid query." });
        }

        LlmQueryEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<LlmQueryEnvelope>(llmOutput, _jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LLM output was not valid JSON. Output: {Output}", llmOutput);
            return Results.BadRequest(new { error = "Invalid JSON format from assistant." });
        }

        if (envelope is null)
        {
            logger.LogWarning("LLM output could not be deserialized into an envelope. Output: {Output}", llmOutput);
            return Results.BadRequest(new { error = "The assistant did not provide a valid response." });
        }

        if (!string.IsNullOrWhiteSpace(envelope.Error))
        {
            logger.LogInformation("LLM returned error for question: {Question}. Error: {Error}", req.Question, envelope.Error);
            return Results.BadRequest(new { error = envelope.Error });
        }

        if (string.IsNullOrWhiteSpace(envelope.Query))
        {
            logger.LogWarning("LLM JSON envelope missing 'query'. Output: {Output}", llmOutput);
            return Results.BadRequest(new { error = "The assistant did not provide a query." });
        }

        if (!IsAllowedEntitySet(envelope.Query))
        {
            logger.LogWarning("Rejected LLM query targeting disallowed entity set. Query: {Query}", envelope.Query);
            return Results.BadRequest(new { error = "Query must target Vehicles or SalesData." });
        }

        try
        {
            logger.LogInformation("Executing OData query (streaming): {Query}", envelope.Query);

            var stream = await executor.ExecuteStreamAsync(envelope.Query, ct);

            var resultType = NormalizeResultType(envelope.ResultType, envelope.Query);
            logger.LogInformation("Success (streaming): {Question} -> {ResultType}", req.Question, resultType);

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.Headers.Append("X-Result-Type", resultType);

            await stream.CopyToAsync(httpContext.Response.Body, ct);

            return Results.Empty;
        }
        catch (TimeoutException tex)
        {
            logger.LogWarning(tex, "Query timed out (streaming) for question: {Question}", req.Question);
            httpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = "The query took too long to execute. Try narrowing your question or adding filters.",
                details = tex.Message
            }, ct);
            return Results.Empty;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.LogInformation("Request cancelled by client (streaming) for question: {Question}", req.Question);
            return Results.Empty;
        }
        catch (HttpRequestException hex)
        {
            logger.LogError(hex, "HTTP error executing OData query (streaming) for question: {Question}", req.Question);
            httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = "Unable to connect to the data service.",
                details = hex.Message
            }, ct);
            return Results.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OData Execution failed (streaming) for question: {Question}", req.Question);
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = "Failed to execute generated query.",
                details = ex.Message
            }, ct);
            return Results.Empty;
        }
    }
}

/// <summary>
/// Request model for natural language query.
/// </summary>
/// <param name="Question">The natural language question to be processed.</param>
public record QueryRequest(string Question);

/// <summary>
/// Envelope for LLM-generated OData query, result type, and error.
/// </summary>
public record LlmQueryEnvelope(
    [property: JsonPropertyName("query")] string? Query,
    [property: JsonPropertyName("resultType")] string? ResultType,
    [property: JsonPropertyName("error")] string? Error
);
