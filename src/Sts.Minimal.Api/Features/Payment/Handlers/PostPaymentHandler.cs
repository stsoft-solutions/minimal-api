using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sts.Minimal.Api.Features.Payment.Model;

namespace Sts.Minimal.Api.Features.Payment.Handlers;

public class PostPaymentHandler
{
    public static async Task<Results<Ok<PostPaymentResponse>, ValidationProblem, ProblemHttpResult>> HandleAsync(
        [FromBody] PostPaymentRequest request, ILogger<PostPaymentHandler> logger
    )
    {
        await Task.Delay(50);

        logger.LogInformation("Posting payment: {@Payment}", request);

        return TypedResults.Ok(new PostPaymentResponse(Guid.NewGuid()));
    }
}