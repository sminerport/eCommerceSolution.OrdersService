using System.Net;

using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

using Polly;
using Polly.Wrap;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies;

public class ProductsMicroservicePolicies : IProductsMicroservicePolicies
{
    private readonly IPollyPolicies _pollyPolicies;

    public ProductsMicroservicePolicies(IPollyPolicies pollyPolicies)
    {
        _pollyPolicies = pollyPolicies;
    }

    public IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        ProductDTO product = new ProductDTO(
            ProductID: Guid.Empty,
            ProductName: "Temporarily Unavailable (fallback)",
            Category: "Temporarily Unavailable (fallback)",
            UnitPrice: 0,
            QuantityInStock: 0);

        var fallbackPolicy = _pollyPolicies.GetFallbackPolicy(
            content: product,
            statusCode: HttpStatusCode.ServiceUnavailable,
            mediaType: "application/json");

        var bulkheadPolicy = _pollyPolicies.GetBulkheadIsolationPolicy(
            maxParallelization: 2, // Allows up to 2 concurrent requests
            maxQueuingActions: 40); // Queue up to 40 additional requests

        AsyncPolicyWrap<HttpResponseMessage> combinedPolicy = Policy.WrapAsync<HttpResponseMessage>(fallbackPolicy, bulkheadPolicy);

        return combinedPolicy;
    }
}