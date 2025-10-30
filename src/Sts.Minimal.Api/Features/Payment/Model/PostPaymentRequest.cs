using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sts.Minimal.Api.Features.Payment.Model;

/// <summary>
/// Represents a request to post a payment.
/// </summary>
/// <remarks>
/// The properties of this class are validated using data annotations to ensure that the request contains valid data.
/// The properties must be nullable to allow the validation framework to check for required fields.
/// </remarks>
public class PostPaymentRequest
{
    [JsonPropertyName("amount")]
    [Required]
    // [Range(0.1, 100.00, ErrorMessage = "Price must be between 0.01 and 100.00")] - we have a bug for values less when 1
    [Range(1, 100.00, ErrorMessage = "Price must be between 1 and 100.00")]
    [Description("The amount of the payment in the specified currency.")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("currency")]
    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter ISO code.")]
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be an ISO 4217 code (e.g., USD).")]
    [Description("The currency code of the payment.")]
    public string? Currency { get; set; }

    [JsonPropertyName("valueDate")]
    [Required]
    [Description("The value date of the payment.")]
    public DateOnly? ValueDate { get; set; }
}