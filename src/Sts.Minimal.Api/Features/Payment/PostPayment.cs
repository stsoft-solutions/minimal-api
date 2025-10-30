using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sts.Minimal.Api.Features.Payment.Model;

namespace Sts.Minimal.Api.Features.Payment;

public static class PostPayment
{
    public static async Task<Results<Ok<PostPaymentResponse>, ValidationProblem, ProblemHttpResult>> HandleAsync(
        [FromBody] PostPaymentRequest request
    )
    {
        await Task.Delay(50);
        return TypedResults.Ok(new PostPaymentResponse(Guid.NewGuid()));
    }
}