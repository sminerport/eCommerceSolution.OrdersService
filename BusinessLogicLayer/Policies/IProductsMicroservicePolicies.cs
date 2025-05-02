using Polly;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies;

public interface IProductsMicroservicePolicies
{
    /// <summary>
    /// Creates a combined policy that includes retry, circuit breaker, timeout, bulkhead isolation, and fallback policies.
    /// </summary>
    /// <returns>Returns an IAsyncPolicy<HttpResponseMessage> that represents the combined policy.</returns>
    IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy();
}