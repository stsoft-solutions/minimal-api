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
    /// <summary>
    /// Gets or sets the amount of the payment in the specified currency.
    /// </summary>
    /// <remarks>
    /// The value must be between 1 and 100.00. This property is required and is validated using
    /// data annotations to ensure correctness. The amount represents the monetary value of the payment
    /// and is expected to be a non-negative decimal number.
    /// </remarks>
    /// <value>
    /// A nullable decimal representing the payment amount.
    /// </value>
    [JsonPropertyName("amount")]
    [Required]
    [Range(1, 100.00, ErrorMessage = "Price must be between 1 and 100.00")]
    [Description("The amount of the payment in the specified currency.")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code of the payment.
    /// </summary>
    /// <remarks>
    /// The currency code must be a 3-letter ISO 4217 code (e.g., USD). This property is required and is validated
    /// to ensure that it is a valid and correctly formatted currency identifier. The value should be in uppercase
    /// alphabetical characters only and exactly 3 characters long.
    /// </remarks>
    /// <value>
    /// A nullable string representing the ISO 4217 currency code for the payment.
    /// </value>
    [JsonPropertyName("currency")]
    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter ISO code.")]
    [RegularExpression("^[A-Z]{3}$", ErrorMessage = "Currency must be an ISO 4217 code (e.g., USD).")]
    [Description("The currency code of the payment.")]
    public string? Currency { get; set; }

    /// <summary>
    /// Gets or sets the value date of the payment.
    /// </summary>
    /// <remarks>
    /// The value date represents the date on which the payment amount is intended to be effective.
    /// This property is required and is validated to ensure a valid date is provided.
    /// </remarks>
    /// <value>
    /// A nullable <see cref="DateOnly" /> representing the effective date of the payment.
    /// </value>
    [JsonPropertyName("value-date")]
    [Required]
    [Description("The value date of the payment.")]
    public DateOnly? ValueDate { get; set; }
}