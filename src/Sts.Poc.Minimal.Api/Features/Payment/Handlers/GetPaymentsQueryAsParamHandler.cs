using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Sts.Poc.Minimal.Api.Features.Payment.Model;

namespace Sts.Poc.Minimal.Api.Features.Payment.Handlers;

/// <summary>
/// Provides a static method for handling a query to retrieve payments using a query parameter object.
/// </summary>
public class GetPaymentsQueryAsParamHandler
{
    /// <summary>
    /// Handles asynchronous processing of the payment query using the specified request parameters.
    /// </summary>
    /// <param name="request">The object containing query parameters for retrieving payments.</param>
    /// <param name="logger">Logger</param>
    /// <returns>
    /// A task that represents the asynchronous operation, returning a result with one of the following outcomes:
    /// - An Ok result containing a collection of <see cref="GetPaymentsItem" /> objects if payments are found.
    /// - A NotFound result if no payments match the criteria.
    /// - A ValidationProblem results if input validation errors occur.
    /// </returns>
    public static Task<Results<Ok<IEnumerable<GetPaymentsItem>>, ValidationProblem, ProblemHttpResult>>
        HandleAsync(
            [AsParameters] GetPaymentsRequest request,
            [FromServices] ILogger<GetPaymentsQueryAsParamHandler> logger
        )
    {
        logger.LogInformation("Get Payments request {@Request}", request);

        return Task.FromResult<Results<Ok<IEnumerable<GetPaymentsItem>>, ValidationProblem, ProblemHttpResult>>(
            TypedResults.Ok(new List<GetPaymentsItem>().AsEnumerable()));
    }
}