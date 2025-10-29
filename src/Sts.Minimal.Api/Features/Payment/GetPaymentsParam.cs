using Microsoft.AspNetCore.Http.HttpResults;

namespace Sts.Minimal.Api.Features.Payment;

/// <summary>
/// Provides a static method for handling a query to retrieve payments using a query parameter object.
/// </summary>
public static class GetPaymentsParam
{
    /// <summary>
    /// Handles asynchronous processing of the payment query using the specified request parameters.
    /// </summary>
    /// <param name="request">The object containing query parameters for retrieving payments.</param>
    /// <returns>
    /// A task that represents the asynchronous operation, returning a result with one of the following outcomes:
    /// - An Ok result containing a collection of <see cref="GetPaymentsItem"/> objects if payments are found.
    /// - A NotFound result if no payments match the criteria.
    /// - A ValidationProblem result if input validation errors occur.
    /// </returns>
    public static async Task<Results<Ok<IEnumerable<GetPaymentsItem>>, NotFound, ValidationProblem>> HandleAsync(
        [AsParameters] GetPaymentsRequest request
    )
    {
        var payments = new List<GetPaymentsItem>();

        return TypedResults.Ok(payments.AsEnumerable());
    }
}