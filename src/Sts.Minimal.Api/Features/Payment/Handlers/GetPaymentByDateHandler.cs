using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sts.Minimal.Api.Features.Payment.Model;

namespace Sts.Minimal.Api.Features.Payment.Handlers;

public class GetPaymentByDateHandler
{
    public static Task<Results<Ok<GetPaymentResponse>, NotFound, ValidationProblem, ProblemHttpResult>> HandleAsync([FromRoute] DateOnly date, ILogger<GetPaymentByDateHandler> logger)
    {
        return Task.FromResult<Results<Ok<GetPaymentResponse>, NotFound, ValidationProblem, ProblemHttpResult>>(TypedResults.Ok(new GetPaymentResponse
        {
            Id = Random.Shared.Next(),
            Amount = 123.43M,
            Currency = "USD",
            Status = PaymentStatus.Completed,
            ReferenceId = Guid.NewGuid(),
            ValueDate = date
        }));
    }
}