using System.Net.Http.Json;
using System.Text.Json;

using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using Polly.CircuitBreaker;
using Polly.Timeout;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;

public class UsersMicroserviceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UsersMicroserviceClient> _logger;
    private readonly IDistributedCache _distributedCache;

    public UsersMicroserviceClient(
        HttpClient httpClient,
        ILogger<UsersMicroserviceClient> logger,
        IDistributedCache distributedCache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task<UserDTO?> GetUserByUserID(Guid userID)
    {
        try
        {
            //Key: user:123
            //Value: { "PersonName: " ... ", ... }
            string cacheKey = $"user:{userID}";
            string? cachedUser = await _distributedCache.GetStringAsync(cacheKey);

            if (cachedUser != null)
            {
                UserDTO? userFromCache = JsonSerializer.Deserialize<UserDTO>(cachedUser);

                if (userFromCache != null)
                {
                    return userFromCache;
                }
            }

            HttpResponseMessage response = await _httpClient.GetAsync($"/gateway/users/{userID}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    UserDTO? userFromFallback = await response.Content.ReadFromJsonAsync<UserDTO>();
                    if (userFromFallback == null)
                    {
                        throw new NotImplementedException("Fallback policy was not implemented.");
                    }
                    return userFromFallback;
                }
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new HttpRequestException("Bad request", null, System.Net.HttpStatusCode.BadRequest);
                }
                else
                {
                    return new UserDTO(
                        PersonName: "Temporarily Unavailable",
                        Email: "Temporarily Unavailable",
                        Gender: "Temporarily Unavailable",
                        UserID: Guid.Empty);
                }
            }

            UserDTO? user = await response.Content.ReadFromJsonAsync<UserDTO>();

            if (user == null)
            {
                throw new ArgumentException("Invalid UserID");
            }

            //Key: user:123
            //Value: { "PersonName: " ... ", ... }

            string userJson = JsonSerializer.Serialize(user);

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(DateTimeOffset.UtcNow.AddMinutes(5));

            await _distributedCache.SetStringAsync(cacheKey, userJson, options);

            return user;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Request failed because the circuit is broken.");

            return new UserDTO(
                PersonName: "Temporarily Unavailable (broken circuit exception)",
                Email: "Temporarily Unavailable (broken circuit exception)",
                Gender: "Temporarily Unavailable (broken circuit exception)",
                UserID: Guid.Empty);
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "Timeout occurred while fetching user data. Returning dummy data");

            return new UserDTO(
                PersonName: "Temporarily Unavailable (timeout exception)",
                Email: "Temporarily Unavailable (timeout exception)",
                Gender: "Temporarily Unavailable (timeout exception)",
                UserID: Guid.Empty);
        }
    }
}