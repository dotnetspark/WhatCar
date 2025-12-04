using System.Text.Json;

namespace WhatCar.Api.Services;

/// <summary>
/// Validates that OData query results contain the required fields for the specified result type.
/// </summary>
public static class ResultValidator
{
    /// <summary>
    /// Validates that the data contains the required fields for the given result type.
    /// </summary>
    /// <param name="resultType">The type of result (ranking, trend, comparison, table).</param>
    /// <param name="data">The JSON data element to validate.</param>
    /// <returns>A validation result with success status and optional error message.</returns>
    public static ValidationResult Validate(string resultType, JsonElement data)
    {
        // Extract the array of items
        var items = data.ValueKind == JsonValueKind.Array
            ? data
            : data.TryGetProperty("value", out var valueProperty)
                ? valueProperty
                : data;

        if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
        {
            return ValidationResult.Success(); // Empty results are valid
        }

        var firstItem = items[0];
        var properties = firstItem.EnumerateObject().Select(p => p.Name).ToList();

        return resultType.ToLowerInvariant() switch
        {
            "ranking" => ValidateRanking(properties),
            "trend" => ValidateTrend(properties),
            "comparison" => ValidateComparison(properties),
            "table" => ValidationResult.Success(), // No specific requirements for table
            _ => ValidationResult.Success() // Unknown types pass through
        };
    }

    private static ValidationResult ValidateRanking(List<string> properties)
    {
        // Need at least one numeric field and one label field
        var hasNumeric = properties.Any(p =>
            p.Contains("units", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("sold", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("value", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("count", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("total", StringComparison.OrdinalIgnoreCase));

        var hasLabel = properties.Any(p =>
            p.Contains("vehicle", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("make", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("model", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("fuel", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("name", StringComparison.OrdinalIgnoreCase));

        if (!hasNumeric)
        {
            return ValidationResult.Failure("Ranking chart requires a numeric field (e.g., UnitsSold, TotalValue). Add it to $select.");
        }

        if (!hasLabel)
        {
            return ValidationResult.Failure("Ranking chart requires a label field (e.g., Make, Model, Fuel). Add it to $expand or $select.");
        }

        return ValidationResult.Success();
    }

    private static ValidationResult ValidateTrend(List<string> properties)
    {
        // Need a time field (Year, Quarter, Date) and a numeric field
        var hasTime = properties.Any(p =>
            p.Contains("year", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("quarter", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("date", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("month", StringComparison.OrdinalIgnoreCase));

        var hasNumeric = properties.Any(p =>
            p.Contains("units", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("sold", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("value", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("count", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("total", StringComparison.OrdinalIgnoreCase));

        if (!hasTime)
        {
            return ValidationResult.Failure("Trend chart requires a time field (e.g., Year, Quarter, Date). Add it to $select.");
        }

        if (!hasNumeric)
        {
            return ValidationResult.Failure("Trend chart requires a numeric field (e.g., UnitsSold). Add it to $select.");
        }

        return ValidationResult.Success();
    }

    private static ValidationResult ValidateComparison(List<string> properties)
    {
        // Need a category field and a numeric field
        var hasCategory = properties.Any(p =>
            p.Contains("vehicle", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("make", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("model", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("fuel", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("type", StringComparison.OrdinalIgnoreCase));

        var hasNumeric = properties.Any(p =>
            p.Contains("units", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("sold", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("value", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("count", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("total", StringComparison.OrdinalIgnoreCase));

        if (!hasCategory)
        {
            return ValidationResult.Failure("Comparison chart requires a category field (e.g., Make, Fuel). Add it to $expand or $select.");
        }

        if (!hasNumeric)
        {
            return ValidationResult.Failure("Comparison chart requires a numeric field (e.g., UnitsSold). Add it to $select.");
        }

        return ValidationResult.Success();
    }
}

/// <summary>
/// Result of validation with success status and optional error message.
/// </summary>
public record ValidationResult(bool IsValid, string? ErrorMessage)
{
    public static ValidationResult Success() => new(true, null);
    public static ValidationResult Failure(string message) => new(false, message);
}
