using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Sts.Minimal.Api.Features.Payment;

public static class GetPayments
{
    public static async Task<Results<Ok<IEnumerable<GetPaymentsItem>>, NotFound, ValidationProblem>> HandleAsync(
        [FromQuery(Name = "paymentId")] [Range(1, 1000)]
        int? paymentId,
        [FromQuery(Name = "valueDate")] DateOnly? valueDate,
        [FromQuery(Name = "status")] PaymentStatus? status
    )
    {
        var payments = new List<GetPaymentsItem>();

        return TypedResults.Ok(payments.AsEnumerable());
    }
}

public static class GetPaymentsParam
{
    public static async Task<Results<Ok<IEnumerable<GetPaymentsItem>>, NotFound, ValidationProblem>> HandleAsync(
        [AsParameters] GetPaymentsRequest request
    )
    {
        var payments = new List<GetPaymentsItem>();

        return TypedResults.Ok(payments.AsEnumerable());
    }
}

public record GetPaymentsRequest(
    [property: Description("Payment ID")]
    [property: Range(1, 1000)]
    int? paymentId,
    [property: Description("Value date")] DateOnly? valueDate,
    [property: Description("Payment status")] PaymentStatus? status);