using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies;

public class PollyPolicies : IPollyPolicies
{
    private readonly ILogger<PollyPolicies> _logger;

    public PollyPolicies(ILogger<PollyPolicies> logger)
    {
        _logger = logger;
    }

    public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int handledEventsAllowedBeforeBreaking, TimeSpan durationOfBreak)
    {
        AsyncCircuitBreakerPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: handledEventsAllowedBeforeBreaking,
                durationOfBreak: durationOfBreak,
                onBreak: (outcome, breakDelay) =>
                    _logger.LogWarning(
                        "Circuit opened for {Minutes}m after {StatusCode}",
                        breakDelay.TotalMinutes, outcome.Result.StatusCode),
                onReset: () =>
                    _logger.LogInformation("Circuit closed; requests will flow again"));

        return policy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
    {
        AsyncRetryPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, delay, attempt, ctx) =>
                {
                    _logger.LogInformation(
                      "Retry {Attempt} after {Delay}s due to {StatusCode}",
                      attempt, delay.TotalSeconds, outcome.Result.StatusCode);
                });

        return policy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
    {
        AsyncTimeoutPolicy<HttpResponseMessage> policy = Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: timeout);

        return policy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetBulkheadIsolationPolicy(int maxParallelization, int maxQueuingActions)
    {
        AsyncBulkheadPolicy<HttpResponseMessage> policy = Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization: maxParallelization, // Allows up to the specified number of concurrent requests
            maxQueuingActions: maxQueuingActions, // Queue up to the specified number of additional requests
            onBulkheadRejectedAsync: (context) =>
            {
                _logger.LogWarning("BulkheadIsolation triggered. Can't send any more requests since the queue is full");

                throw new BulkheadRejectedException("Bulkhead queue is full");
            });

        return policy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy(object content, HttpStatusCode statusCode, string mediaType)
    {
        AsyncFallbackPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .FallbackAsync(async (context) =>
            {
                _logger.LogInformation("Fallback policy executed. Returning default response.");

                HttpResponseMessage response = new HttpResponseMessage(statusCode: HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent(
                        content: JsonSerializer.Serialize(content),
                        encoding: Encoding.UTF8,
                        mediaType: mediaType)
                };

                return response;
            });

        return policy;
    }
}