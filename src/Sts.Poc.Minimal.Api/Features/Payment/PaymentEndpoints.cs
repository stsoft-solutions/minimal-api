using Scalar.AspNetCore;
using Sts.Poc.Minimal.Api.Features.Payment.Handlers;
using Sts.Poc.Minimal.Api.Features.Payment.Model;
using Sts.Poc.Minimal.Api.Infrastructure.Auth;

namespace Sts.Poc.Minimal.Api.Features.Payment;

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
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(AuthorizationConstants.Policies.Reader);

        // GET by id should be public (anonymous)
        group.MapGet("/{paymentId:int}", GetPaymentHandler.HandleAsync)
            .AllowAnonymous()
            .WithName("GetPayment")
            .WithDescription("Retrieves payment information by payment ID.")
            .Produces<GetPaymentResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Stable();

        // GET by referenceId should be public (anonymous)
        group.MapGet("/by-reference/{referenceId:guid}", GetPaymentByReferenceHandler.HandleAsync)
            .AllowAnonymous()
            .WithName("GetPaymentByReference")
            .WithDescription("Retrieves payment information by reference ID.")
            .Produces<GetPaymentResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Stable();

        // GET by date should be public (anonymous)
        group.MapGet("/by-date/{date}", GetPaymentByDateHandler.HandleAsync)
            .AllowAnonymous()
            .WithName("GetPaymentByDate")
            .WithDescription("Retrieves payment information by payment date.")
            .Produces<GetPaymentResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Stable();

        // GET endpoints require 'reader' role
        group.MapGet("/query", GetPaymentsQueryHandler.HandleAsync)
            .WithName("GetPaymentsQuery")
            .WithDescription("Retrieves payments information using query parameters. Requires role 'reader'.")
            .Produces<GetPaymentsItem>()
            .Experimental();

        group.MapGet("/query-param", GetPaymentsQueryAsParamHandler.HandleAsync)
            .WithName("GetPaymentsQueryAsParam")
            .WithDescription("Retrieves payments information using a query parameter object. Requires role 'reader'.")
            .Produces<GetPaymentsItem>()
            .Stable();

        // POST endpoint requires 'writer' role
        group.MapPost("/", PostPaymentHandler.HandleAsync)
            .RequireAuthorization(AuthorizationConstants.Policies.Writer)
            .WithName("PostPayment")
            .WithDescription("Processes a new payment. Requires role 'writer'.")
            .Produces<PostPaymentResponse>()
            .Stable();

        return group;
    }
}