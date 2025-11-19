using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Sts.Poc.Minimal.Api.Infrastructure.Validation.Attributes;

/// <summary>
/// Validates that the value is either null/empty or a valid ISO date in yyyy-MM-dd format.
/// Intended for Minimal API query parameters bound as strings to avoid binder exceptions
/// when the incoming value isn't a valid date.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class StringAsIsoDateAttribute : ValidationAttribute
{
    public StringAsIsoDateAttribute()
    {
        // The default message follows MVC-style wording used by built-in model binding errors
        // Example: The value '2025-31-31' is not valid for valueDate.
        ErrorMessage = "The value '{0}' is not valid for {1}.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            // null is allowed (use [Required] if needed)
            return ValidationResult.Success;

        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return ValidationResult.Success;

            // Validate strictly against ISO format yyyy-MM-dd
            if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _))
                return ValidationResult.Success;

            var memberName = validationContext.MemberName ?? "value";
            var message = string.Format(ErrorMessageString, s, memberName);
            return new ValidationResult(message);
        }

        // Non-string values are considered invalid for this attribute
        var mName = validationContext.MemberName ?? "value";
        var msg = string.Format(ErrorMessageString, value, mName);
        return new ValidationResult(msg);
    }
}