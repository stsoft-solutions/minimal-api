using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Sts.Minimal.Api.Features.Payment;

public static class GetPayments
{
    public static async Task<Results<Ok<IEnumerable<GetPaymentsItem>>, NotFound, ValidationProblem>> HandleAsync(
        [FromQuery(Name = "paymentId")] [Range(1, 1000)] int? paymentId ,
        [FromQuery(Name = "valueDate")] DateOnly? valueDate,
        [FromQuery(Name = "status")] PaymentStatus? status
    )
    {
        var payments = new List<GetPaymentsItem>();
        
        return TypedResults.Ok(payments.AsEnumerable());
    }
}