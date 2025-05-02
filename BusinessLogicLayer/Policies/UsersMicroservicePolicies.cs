using Polly;
using Polly.Wrap;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies;

public class UsersMicroservicePolicies : IUsersMicroservicePolicies
{
    private readonly IPollyPolicies _pollyPolicies;

    public UsersMicroservicePolicies(IPollyPolicies pollyPolicies)
    {
        _pollyPolicies = pollyPolicies;
    }

    public IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        var retryPolicy = _pollyPolicies.GetRetryPolicy(5);

        var circuitBreakerPolicy = _pollyPolicies.GetCircuitBreakerPolicy(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromMinutes(2));

        var timeoutPolicy = _pollyPolicies.GetTimeoutPolicy(TimeSpan.FromSeconds(30));

        AsyncPolicyWrap<HttpResponseMessage> wrappedPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);

        return wrappedPolicy;
    }
}