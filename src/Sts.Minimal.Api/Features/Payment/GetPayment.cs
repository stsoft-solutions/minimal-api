using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sts.Minimal.Api.Features.Payment.Model;

namespace Sts.Minimal.Api.Features.Payment;

/// <summary>
/// Provides a method to handle a GET payment request by payment ID.
/// </summary>
public static class GetPayment
{
    /// <summary>
    /// Handles the GET payment request by retrieving payment information based on the provided payment ID.
    /// </summary>
    /// <param name="paymentId">The ID of the payment to retrieve. Must be a number between 1 and 1000.</param>
    /// <returns>
    /// A <see cref="Results" /> object that can represent the following outcomes:
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///         An <see cref="Ok{TValue}" /> result containing <see cref="GetPaymentResponse" /> if the payment is
    ///         found.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>A <see cref="NotFound" /> result if the payment is not found.</description>
    ///     </item>
    ///     <item>
    ///         <description>A <see cref="ValidationProblem" /> result if the input data is invalid.</description>
    ///     </item>
    /// </list>
    /// </returns>
    public static async Task<Results<Ok<GetPaymentResponse>, NotFound, ValidationProblem>> HandleAsync(
        [FromRoute(Name = "paymentId")]
        [Required]
        [Range(1, 1000)]
        [Description("The ID of the payment to retrieve. Must be a number between 1 and 1000.")]
        int paymentId
    )
    {
        // Small delay to simulate async operation
        await Task.Delay(50);

        switch (paymentId)
        {
            case 666:
            {
                // Validate paymentId
                var errors = new Dictionary<string, string[]>
                {
                    { "paymentId", ["Payment ID cannot be 666"] }
                };

                return TypedResults.ValidationProblem(errors);
            }
            case 1:
                // Simulate fetching payment from a data source
                return TypedResults.Ok(new GetPaymentResponse
                {
                    Id = paymentId,
                    Amount = 123.43M,
                    Currency = "USD",
                    Status = PaymentStatus.Completed,
                    ReferenceId = Guid.NewGuid(),
                    ValueDate = DateOnly.FromDateTime(DateTime.UtcNow)
                });
            default:
                return TypedResults.NotFound();
        }
    }
}