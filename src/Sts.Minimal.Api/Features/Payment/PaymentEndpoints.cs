using Microsoft.AspNetCore.Http.HttpResults;
using Sts.Minimal.Api.Infrastructure.Validation;

namespace Sts.Minimal.Api.Features.Payment;

/// <summary>
/// Provides extension methods for mapping payment-related API endpoints.
/// </summary>
public static class PaymentEndpoints
{
    /// <summary>
    /// Maps the payment-related API endpoints to the provided route builder.
    /// </summary>
    /// <param name="routes">The <see cref="IEndpointRouteBuilder"/> used to define the payment API endpoints.</param>
    /// <returns>A <see cref="RouteGroupBuilder"/> that represents the mapped payment endpoints.</returns>
    public static RouteGroupBuilder MapPaymentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/payments")
            .WithTags("Payment");

        group.MapGet("/{paymentId:int}", GetPayment.HandleAsync)
            .AddDataAnnotationsValidation()
            .WithName("GetPayment")
            .Produces<GetPaymentResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/", GetPayments.HandleAsync)
            .AddDataAnnotationsValidation()
            .WithName("GetPayments")
            .Produces<GetPaymentsItem>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        
        return group;
    }
}