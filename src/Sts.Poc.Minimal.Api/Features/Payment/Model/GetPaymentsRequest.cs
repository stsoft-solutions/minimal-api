using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Sts.Poc.Minimal.Api.Infrastructure.Validation.Attributes;

namespace Sts.Poc.Minimal.Api.Features.Payment.Model;

/// <summary>
/// Represents a request to retrieve payment records based on specific query parameters.
/// </summary>
/// <remarks>
/// This record is used to encapsulate the query parameters needed to filter payments.
/// The parameters include an optional payment ID, an optional value date, and an optional payment status.
/// </remarks>
[UsedImplicitly]
public record GetPaymentsRequest(
    [property: Description("Payment ID")]
    [property: Range(1, 1000)]
    [property: FromQuery(Name = "payment-id")]
    int? PaymentId,
    [property: Description("Value date")]
    [property: FromQuery(Name = "value-date")]
    [property: StringAsIsoDate]
    string? ValueDateRaw,
    [property: Description("Payment status")]
    [property: FromQuery(Name = "status")]
    PaymentStatus? Status
)
{
    /// <summary>
    /// Parsed value of <c>value-date</c> when provided in ISO format (yyyy-MM-dd). Returns null if missing or invalid.
    /// </summary>
    public DateOnly? ValueDate
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ValueDateRaw)) return null;
            return DateOnly.TryParseExact(ValueDateRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var d)
                ? d
                : null;
        }
    }
}