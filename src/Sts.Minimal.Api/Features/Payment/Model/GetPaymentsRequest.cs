using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;

namespace Sts.Minimal.Api.Features.Payment.Model;

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
    DateOnly? ValueDate,
    [property: Description("Payment status")]
    [property: FromQuery(Name = "status")]
    PaymentStatus? Status
);