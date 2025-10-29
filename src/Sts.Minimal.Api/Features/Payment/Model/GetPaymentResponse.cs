namespace Sts.Minimal.Api.Features.Payment.Model;

/// <summary>
/// Represents the response to a payment retrieval request.
/// Contains details about the payment such as its status, amount, and associated metadata.
/// </summary>
public sealed class GetPaymentResponse
{
    public int Id { get; set; }
    public PaymentStatus Status { get; set; }
    public Guid ReferenceId { get; set; }
    public DateOnly ValueDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}