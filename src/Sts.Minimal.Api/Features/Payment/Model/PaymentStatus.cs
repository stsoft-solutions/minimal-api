using System.Text.Json.Serialization;

namespace Sts.Minimal.Api.Features.Payment.Model;

/// <summary>
/// Represents the status of a payment.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PaymentStatus>))]
public enum PaymentStatus
{
    Pending,
    [JsonStringEnumMemberName("FINISHED")] Completed,
    Failed
}