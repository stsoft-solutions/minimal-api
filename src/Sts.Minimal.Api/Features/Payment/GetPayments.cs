using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sts.Minimal.Api.Features.Payment.Model;
using Sts.Minimal.Api.Infrastructure.Serialization;
using Sts.Minimal.Api.Infrastructure.Validation;

namespace Sts.Minimal.Api.Features.Payment;

/// <summary>
/// Provides functionality to retrieve payment information based on query parameters.
/// </summary>
public class GetPayments
{
    public static async Task<Results<Ok<IEnumerable<GetPaymentsItem>>, NotFound, ValidationProblem, ProblemHttpResult>>
        HandleAsync(
            [FromQuery(Name = "paymentId")] [Range(1, 1000)] [Description("Payment ID")]
            int? paymentId,
            [FromQuery(Name = "valueDate")] [Description("Value date")] [IsoDateOnly]
            string? valueDate,
            [FromQuery(Name = "status")] [Description("Payment's status")] [EnumString(typeof(PaymentStatus))]
            string? rawStatus,
            [FromServices] ILogger<GetPayments> logger
        )
    {
        // Small delay to simulate async operation
        await Task.Delay(50);

        // Normalize status to enum if provided (supports JsonStringEnumMemberName)
        var parsedStatus = EnumParsing.ParseNullable<PaymentStatus>(rawStatus);

        logger.LogInformation("Fetching payments with PaymentId: {PaymentId}, ValueDate: {ValueDate}, Status: {Status}", paymentId,
            valueDate, parsedStatus);

        // Here you would typically filter payments based on the provided parameters.
        return TypedResults.Ok(new List<GetPaymentsItem>().AsEnumerable());
    }
}