namespace Sts.Poc.Minimal.Api.Features.Payment.Model;

/// <summary>
/// Represents a payment item in the API response.
/// </summary>
public class GetPaymentsItem
{
    /// <summary>
    /// Gets or sets the unique identifier of the payment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the status of the payment.
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the value date of the payment.
    /// </summary>
    public DateOnly ValueDate { get; set; }

    /// <summary>
    /// Gets or sets the amount of the payment.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency of the payment.
    /// </summary>
    public string Currency { get; set; } = string.Empty;
}