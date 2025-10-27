using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;

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
            .AddEndpointFilterFactory((factoryContext, next) =>
            {
                var parameters = factoryContext.MethodInfo?.GetParameters() ?? [];
                return async invocationContext =>
                {
                    Dictionary<string, List<string>>? errors = null;
        
                    for (var i = 0; i < parameters.Length && i < invocationContext.Arguments.Count; i++)
                    {
                        var parameter = parameters[i];
                        var value = invocationContext.Arguments[i];
        
                        var attrs = parameter
                            .GetCustomAttributes(typeof(ValidationAttribute), inherit: true)
                            .OfType<ValidationAttribute>()
                            .ToArray();
        
                        if (attrs.Length == 0)
                            continue;
        
                        var memberName = parameter.Name ?? $"arg{i}";
        
                        foreach (var attr in attrs)
                        {
                            var validationContext = new ValidationContext(value ?? new object())
                            {
                                MemberName = memberName
                            };
        
                            var result = attr.GetValidationResult(value, validationContext);
                            if (result != ValidationResult.Success)
                            {
                                errors ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                                if (!errors.TryGetValue(memberName, out var list))
                                {
                                    list = new List<string>();
                                    errors[memberName] = list;
                                }
        
                                var message = result?.ErrorMessage ?? attr.FormatErrorMessage(memberName);
                                if (!string.IsNullOrWhiteSpace(message))
                                {
                                    list.Add(message);
                                }
                            }
                        }
                    }
        
                    if (errors is not null && errors.Count > 0)
                    {
                        var dict = errors.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Count == 0 ? [] : kvp.Value.ToArray(),
                            StringComparer.OrdinalIgnoreCase);
        
                        return TypedResults.ValidationProblem(dict);
                    }
        
                    return await next(invocationContext);
                };
            })
            .WithName("GetPayment")
            .Produces<GetPaymentResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        
        return group;
    }
}