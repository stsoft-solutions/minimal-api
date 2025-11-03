using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sts.Minimal.Api.Features.Payment.Model;
using Sts.Minimal.Api.Infrastructure.Serialization;
using Sts.Minimal.Api.Infrastructure.Validation.Attributes;

namespace Sts.Minimal.Api.Features.Payment;

/// <summary>
/// Provides functionality to retrieve payment information based on query parameters.
/// </summary>
public class GetPaymentsQuery
{
    public static async Task<Results<Ok<IEnumerable<GetPaymentsItem>>, NotFound, ValidationProblem, ProblemHttpResult>>
        HandleAsync(
            [FromQuery(Name = "paymentId")] [Range(1, 1000)] [Description("Payment ID")]
            int? paymentId,
            [FromQuery(Name = "valueDateString")] [Description("Value date")] [StringAsIsoDate]
            string? valueDateString,
            [FromQuery(Name = "status")] [Description("Payment's status")] [StringAsEnum(typeof(PaymentStatus))]
            string? rawStatus,
            [FromQuery(Name = "referenceId")] [Description("Reference ID")]
            Guid? referenceId,
            [FromServices] ILogger<GetPaymentsQuery> logger
        )
    {
        // Small delay to simulate async operation
        await Task.Delay(50);

        // Normalize status to enum if provided (supports JsonStringEnumMemberName)
        var parsedStatus = EnumParsing.ParseNullable<PaymentStatus>(rawStatus);

        logger.LogInformation(
            "Fetching payments with PaymentId: {PaymentId}, ValueDate: {ValueDate}, Status: {Status}, ReferenceId: {ReferenceId}",
            paymentId, valueDateString, parsedStatus, referenceId);

        // Here you would typically filter payments based on the provided parameters.
        return TypedResults.Ok(new List<GetPaymentsItem>().AsEnumerable());
    }
}