using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sts.Minimal.Api.Features.Payment.Model;

namespace Sts.Minimal.Api.Features.Payment;

public class PostPayment
{
    public static async Task<Results<Ok<PostPaymentResponse>, ValidationProblem, ProblemHttpResult>> HandleAsync(
        [FromBody] PostPaymentRequest request, ILogger<PostPayment> logger
    )
    {
        await Task.Delay(50);
        
        logger.LogInformation("Posting payment: {@Payment}", request);
        
        return TypedResults.Ok(new PostPaymentResponse(Guid.NewGuid()));
    }
}