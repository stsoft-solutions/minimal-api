using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sts.Poc.Minimal.Api.Features.Payment.Model;

namespace Sts.Poc.Minimal.Api.Features.Payment.Handlers;

public class GetPaymentByReferenceHandler
{
    public static Task<Results<Ok<GetPaymentResponse>, NotFound, ValidationProblem, ProblemHttpResult>> HandleAsync(
        [FromRoute] Guid referenceId, ILogger<GetPaymentByReferenceHandler> logger)
    {
        return Task.FromResult<Results<Ok<GetPaymentResponse>, NotFound, ValidationProblem, ProblemHttpResult>>(TypedResults.Ok(
            new GetPaymentResponse
            {
                Id = 223,
                Amount = 123.43M,
                Currency = "USD",
                Status = PaymentStatus.Completed,
                ReferenceId = referenceId,
                ValueDate = DateOnly.FromDateTime(DateTime.UtcNow)
            }));
    }
}