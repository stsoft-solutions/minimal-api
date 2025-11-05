using Scalar.AspNetCore;
using Sts.Minimal.Api.Features.Payment.Handlers;
using Sts.Minimal.Api.Features.Payment.Model;
using Sts.Minimal.Api.Infrastructure.Auth;
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

        // GET by id should be public (anonymous)
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

        // GET endpoints require 'reader' role
        group.MapGet("/query", GetPaymentsQueryHandler.HandleAsync)
            .RequireAuthorization(AuthorizationConstants.Policies.Reader)
            .AddDataAnnotationsValidation()
            .WithName("GetPaymentsQuery")
            .WithDescription("Retrieves payments information using query parameters. Requires role 'reader'.")
            .Produces<GetPaymentsItem>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Experimental();

        group.MapGet("/query-param", GetPaymentsQueryAsParamHandler.HandleAsync)
            .RequireAuthorization(AuthorizationConstants.Policies.Reader)
            .AddDataAnnotationsValidation()
            .WithName("GetPaymentsQueryAsParam")
            .WithDescription("Retrieves payments information using a query parameter object. Requires role 'reader'.")
            .Produces<GetPaymentsItem>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Stable();

        // POST endpoint requires 'writer' role
        group.MapPost("/", PostPaymentHandler.HandleAsync)
            .RequireAuthorization(AuthorizationConstants.Policies.Writer)
            .AddDataAnnotationsValidation()
            .WithName("PostPayment")
            .WithDescription("Processes a new payment. Requires role 'writer'.")
            .Produces<PostPaymentResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Stable();


        return group;
    }
}