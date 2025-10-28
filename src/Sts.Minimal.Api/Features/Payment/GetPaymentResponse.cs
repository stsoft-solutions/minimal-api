namespace Sts.Minimal.Api.Features.Payment;

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