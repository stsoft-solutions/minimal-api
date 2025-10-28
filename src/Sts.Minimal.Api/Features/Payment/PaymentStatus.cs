using System.Text.Json.Serialization;

namespace Sts.Minimal.Api.Features.Payment;

[JsonConverter(typeof(JsonStringEnumConverter<PaymentStatus>))]
public enum PaymentStatus
{
    Pending,
    [JsonStringEnumMemberName("FINISHED")] Completed,
    Failed
}