using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sts.Minimal.Api.Features.Payment.Model;

namespace Sts.Minimal.Api.Features.Payment;

/// <summary>
/// Provides functionality to retrieve payment information based on query parameters.
/// </summary>
public static class GetPayments
{
    public static async Task<Results<Ok<IEnumerable<GetPaymentsItem>>, NotFound, ValidationProblem, ProblemHttpResult>>
        HandleAsync(
            [FromQuery(Name = "paymentId")] [Range(1, 1000)] [Description("Payment ID")]
            int? paymentId,
            [FromQuery(Name = "valueDate")] [Description("Value date")]
            DateOnly? valueDate,
            [FromQuery(Name = "status")] [Description("Payment's status")]
            PaymentStatus? status
        )
    {
        // Small delay to simulate async operation
        await Task.Delay(50);

        // Simulate fetching payments from a data source
        var payments = new List<GetPaymentsItem>();

        // Here you would typically filter payments based on the provided parameters.
        return TypedResults.Ok(payments.AsEnumerable());
    }
}