using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Polly;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies;

public interface IPollyPolicies
{
    /// <summary>
    /// Creates a retry policy that retries the request a specified number of times.
    /// </summary>
    /// <param name="retryCount">The number of times to retry the request.</param>
    /// <returns>Returns an IAsyncPolicy<HttpResponseMessage> that represents the retry policy.</returns>
    IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount);

    /// <summary>
    /// Creates a circuit breaker policy that breaks the circuit after a specified number of handled events.
    /// </summary>
    /// <param name="handledEventsAllowedBeforeBreaking">The number of handled events allowed before breaking the circuit.</param>
    /// <param name="durationOfBreak">The duration of the break.</param>
    /// <returns>Returns an IAsyncPolicy<HttpResponseMessage> that represents the circuit breaker policy.</returns>
    IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int handledEventsAllowedBeforeBreaking, TimeSpan durationOfBreak);

    /// <summary>
    /// Creates a timeout policy that cancels the request if it takes longer than the specified timeout.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>Returns an IAsyncPolicy<HttpResponseMessage> that represents the timeout policy.</returns>
    IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout);

    /// <summary>
    /// Creates a bulkhead isolation policy that limits the number of concurrent requests and queued requests.
    /// </summary>
    /// <param name="maxParallelization">The maximum number of concurrent requests allowed.</param>
    /// <param name="maxQueuingActions">The maximum number of requests that can be queued.</param>
    /// <returns>Returns an IAsyncPolicy<HttpResponseMessage> that represents the bulkhead isolation policy.</returns>
    IAsyncPolicy<HttpResponseMessage> GetBulkheadIsolationPolicy(int maxParallelization, int maxQueuingActions);

    /// <summary>
    /// Creates a fallback policy that returns a default response when the original request fails.
    /// </summary>
    /// <param name="content">The content to return in the fallback response.</param>
    /// <param name="statusCode">The HTTP status code to return in the fallback response.</param>
    /// <param name="mediaType">The media type of the content.</param>
    /// <returns>Returns an IAsyncPolicy<HttpResponseMessage> that represents the fallback policy.</returns>
    IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy(Object content, HttpStatusCode statusCode, string mediaType);
}