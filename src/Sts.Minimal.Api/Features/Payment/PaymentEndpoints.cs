using Scalar.AspNetCore;
using Sts.Minimal.Api.Features.Payment.Handlers;
using Sts.Minimal.Api.Features.Payment.Model;
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
    /// <param name="routes">The <see cref="IEndpointRouteBuilder" /> used to define the payment API endpoints.</param>
    /// <returns>A <see cref="RouteGroupBuilder" /> that represents the mapped payment endpoints.</returns>
    public static RouteGroupBuilder MapPaymentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/payments")
            .WithTags("Payment")
            .RequireAuthorization();

        group.MapGet("/{paymentId:int}", GetPaymentHandler.HandleAsync)
            .AllowAnonymous()
            .AddDataAnnotationsValidation()
            .WithName("GetPayment")
            .WithDescription("Retrieves payment information by payment ID.")
            .Produces<GetPaymentResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Stable();

        group.MapGet("/query", GetPaymentsQueryHandler.HandleAsync)
            .AddDataAnnotationsValidation()
            .WithName("GetPaymentsQuery")
            .WithDescription("Retrieves payments information using query parameters.")
            .Produces<GetPaymentsItem>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Experimental();

        group.MapGet("/query-param", GetPaymentsQueryAsParamHandler.HandleAsync)
            .AddDataAnnotationsValidation()
            .WithName("GetPaymentsQueryAsParam")
            .WithDescription("Retrieves payments information using a query parameter object.")
            .Produces<GetPaymentsItem>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Stable();

        group.MapPost("/", PostPaymentHandler.HandleAsync)
            .AddDataAnnotationsValidation()
            .WithName("PostPayment")
            .WithDescription("Processes a new payment.")
            .Produces<PostPaymentResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Stable();


        return group;
    }
}