using System.Text.Json;

namespace Sts.Minimal.Api.Infrastructure.Serialization;

/// <summary>
/// Helpers for parsing enum values from raw strings while honoring
/// System.Text.Json attributes like <see cref="System.Text.Json.Serialization.JsonStringEnumMemberNameAttribute" />.
/// </summary>
public static class EnumParsing
{
    /// <summary>
    /// Tries to parse the provided <paramref name="raw" /> string as an enum value of <typeparamref name="TEnum" />.
    /// Parsing first attempts <see cref="JsonSerializer" /> deserialization to respect
    /// <c>JsonStringEnumMemberNameAttribute</c>, then falls back to case-insensitive
    /// <see cref="Enum.TryParse{TEnum}(string,bool,out TEnum)" />.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="raw">The raw string value to parse.</param>
    /// <param name="value">The parsed enum value when parsing succeeds; otherwise default.</param>
    /// <returns><c>true</c> if parsing succeeds; otherwise <c>false</c>.</returns>
    public static bool TryParse<TEnum>(string? raw, out TEnum value) where TEnum : struct, Enum
    {
        value = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        // Attempt JSON-based parsing to honor JsonStringEnumMemberNameAttribute
        try
        {
            var deserialized = JsonSerializer.Deserialize<TEnum>($"\"{raw}\"");
            if (!deserialized.Equals(default(TEnum)) || Enum.IsDefined(typeof(TEnum), deserialized))
            {
                value = deserialized;
                return true;
            }
        }
        catch
        {
            // ignore and fallback to Enum.TryParse
        }

        // Fallback to classic enum parsing (case-insensitive)
        if (Enum.TryParse<TEnum>(raw, true, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses the provided <paramref name="raw" /> as a nullable enum value of <typeparamref name="TEnum" />.
    /// Returns <c>null</c> when input is null/whitespace or parsing fails.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="raw">The raw string value to parse.</param>
    /// <returns>The parsed enum value or <c>null</c>.</returns>
    public static TEnum? ParseNullable<TEnum>(string? raw) where TEnum : struct, Enum
    {
        return TryParse<TEnum>(raw, out var value) ? value : null;
    }
}