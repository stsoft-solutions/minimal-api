using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sts.Minimal.Api.Features.Payment.Model;
using Sts.Minimal.Api.Infrastructure.Serialization;
using Sts.Minimal.Api.Infrastructure.Validation.Attributes;

namespace Sts.Minimal.Api.Features.Payment.Handlers;

/// <summary>
/// Provides functionality to retrieve payment information based on query parameters.
/// </summary>
public class GetPaymentsQueryHandler
{
    public static async Task<Results<Ok<IEnumerable<GetPaymentsItem>>, NotFound, ValidationProblem, ProblemHttpResult>>
        HandleAsync(
            [FromQuery] [Range(1, 1000)] [Description("Payment ID")]
            int? paymentId,
            [FromQuery] [Description("Value date")] [StringAsIsoDate]
            string? valueDateString,
            [FromQuery] [Description("Payment's status")] [StringAsEnum(typeof(PaymentStatus))]
            string? status,
            [FromQuery] [Description("Reference ID")]
            Guid? referenceId,
            [FromQuery] [Description("Value date 1")]
            DateOnly? valueDate,
            [FromQuery] [Description("Payment's status")]
            PaymentStatus? statusEnumNullable,
            [FromQuery] [Description("Payment's status")]
            PaymentStatus statusEnum,
            [FromQuery(Name = "custom-status")] [Description("Payment's status")]
            PaymentStatus statusEnumCustomName,
            [FromServices] ILogger<GetPaymentsQueryHandler> logger,
            CancellationToken cancellationToken = default
        )
    {
        // Small delay to simulate async operation
        await Task.Delay(50, cancellationToken);

        // Normalize status to enum if provided (supports JsonStringEnumMemberName)
        var parsedStatus = EnumParsing.ParseNullable<PaymentStatus>(status);

        logger.LogInformation(
            "Fetching payments with PaymentId: {PaymentId}, ValueDate: {ValueDate}, Status: {Status}, ReferenceId: {ReferenceId}, ValueDateParam: {ValueDateParam}",
            paymentId, valueDateString, parsedStatus, referenceId, valueDate);

        // Here you would typically filter payments based on the provided parameters.
        return TypedResults.Ok(new List<GetPaymentsItem>().AsEnumerable());
    }
}