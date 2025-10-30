using System.Text.Json.Serialization;

namespace Sts.Minimal.Api.Features.Payment.Model;

public record PostPaymentResponse(
    [property: JsonPropertyName("paymentId")]
    Guid Id
);