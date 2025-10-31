using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sts.Minimal.Api.Features.Payment.Model;

/// <summary>
/// Represents the response to a payment retrieval request.
/// Contains details about the payment such as its status, amount, and associated metadata.
/// </summary>
public sealed class GetPaymentResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the payment.
    /// Typically used to reference and retrieve specific payment details.
    /// </summary>
    [JsonPropertyName("id")]
    public required int Id { get; set; }

    /// <summary>
    /// Gets or sets the status of the payment.
    /// Indicates whether the payment is pending, completed, or failed.
    /// </summary>
    [JsonPropertyName("status")]
    public required PaymentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the reference identifier associated with the payment.
    /// Commonly used for correlation or tracking across systems.
    /// </summary>
    [JsonPropertyName("referenceId")]
    public required Guid ReferenceId { get; set; }

    /// <summary>
    /// Gets or sets the value date of the payment.
    /// Represents the date on which the payment is considered effective,
    /// often used for settlement or accounting purposes.
    /// </summary>
    [JsonPropertyName("value-date")]
    public required DateOnly ValueDate { get; set; }

    /// <summary>
    /// Gets or sets the monetary value associated with the payment.
    /// Represents the total amount to be settled in the specified currency.
    /// </summary>
    [JsonPropertyName("amount")]
    public required decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency associated with the payment.
    /// Represents the ISO 4217 currency code (e.g., USD, EUR) in which the payment is made.
    /// </summary>
    [JsonPropertyName("currency")]
    public required string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp indicating when the payment was last updated or processed.
    /// This property is useful for tracking and auditing purposes.
    /// </summary>
    [Required]
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}