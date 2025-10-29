namespace Sts.Minimal.Api.Features.Payment;

public class GetPaymentsItem
{
    public Guid Id { get; set; }
    public PaymentStatus Status { get; set; }
    public DateOnly ValueDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}