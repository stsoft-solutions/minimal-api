using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Sts.Poc.Minimal.Api.Infrastructure.Validation.Attributes;

/// <summary>
/// Validates that a string (typically a Minimal API query parameter) is either empty/null
/// or matches one of the enum names or their <see cref="JsonStringEnumMemberNameAttribute" /> values
/// for the specified enum type. This avoids model binding exceptions for invalid enum values
/// and allows returning standard validation problem details (400).
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class StringAsEnumAttribute : ValidationAttribute
{
    private readonly HashSet<string> _allowedValues;

    public StringAsEnumAttribute(Type enumType)
    {
        if (enumType is null) throw new ArgumentNullException(nameof(enumType));
        if (!enumType.IsEnum) throw new ArgumentException("Type must be an enum", nameof(enumType));

        EnumType = enumType;
        ErrorMessage = "The value '{0}' is not valid for {1}.";
        _allowedValues = BuildAllowedValues(enumType);
    }

    /// <summary>
    /// The enum type this attribute validates against.
    /// Exposed for tooling and OpenAPI transformers.
    /// </summary>
    public Type EnumType { get; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null) return ValidationResult.Success;
        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;

            // Compare case-insensitively for both enum names and JsonStringEnumMemberName values.
            if (_allowedValues.Contains(s)) return ValidationResult.Success;

            var memberName = validationContext.MemberName ?? "value";
            var message = string.Format(ErrorMessageString, s, memberName);
            return new ValidationResult(message);
        }

        // Non-string -> invalid
        var mName = validationContext.MemberName ?? "value";
        var msg = string.Format(ErrorMessageString, value, mName);
        return new ValidationResult(msg);
    }

    private static HashSet<string> BuildAllowedValues(Type enumType)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add enum names
        foreach (var name in Enum.GetNames(enumType))
        {
            set.Add(name);
        }

        // Add JsonStringEnumMemberName values when present
        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var f in fields)
        {
            var jsonNameAttr = f.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
            if (jsonNameAttr?.Name is { Length: > 0 } custom) set.Add(custom);
        }

        return set;
    }
}