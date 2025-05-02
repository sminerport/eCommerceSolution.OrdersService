using System.Net.Http.Json;
using System.Text.Json;

using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using Polly.Bulkhead;

namespace BusinessLogicLayer.HttpClients
{
    public class ProductMicroserviceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductMicroserviceClient> _logger;
        private readonly IDistributedCache _distributedCache;

        public ProductMicroserviceClient(
            HttpClient httpClient,
            ILogger<ProductMicroserviceClient> logger,
            IDistributedCache distributedCache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _distributedCache = distributedCache;
        }

        public async Task<ProductDTO?> GetProductByProductID(Guid productID)
        {
            try
            {
                //Key: product:123
                //Value: { "ProductName: " ... ", ... }

                string cacheKey = $"product:{productID}";
                string? cachedProduct = await _distributedCache.GetStringAsync(cacheKey);

                if (cachedProduct != null)
                {
                    ProductDTO? productFromCache = JsonSerializer.Deserialize<ProductDTO>(cachedProduct);

                    if (productFromCache != null)
                    {
                        _logger.LogInformation($"Product found in cache: {productFromCache.ProductID}, {productFromCache.ProductName}, {productFromCache.Category}, {productFromCache.UnitPrice}, {productFromCache.QuantityInStock}");
                        return productFromCache;
                    }
                }

                HttpResponseMessage response = await _httpClient.GetAsync($"/gateway/products/search/product-id/{productID}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                    {
                        ProductDTO? productFromFallback = await response.Content.ReadFromJsonAsync<ProductDTO>();

                        if (productFromFallback == null)
                        {
                            throw new NotImplementedException("Fallback policy was not implemented.");
                        }

                        return productFromFallback;
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new HttpRequestException("Product not found", null, System.Net.HttpStatusCode.NotFound);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        throw new HttpRequestException("Bad request", null, System.Net.HttpStatusCode.BadRequest);
                    }
                    else
                    {
                        throw new HttpRequestException($"Http request failed with status code {response.StatusCode}");
                    }
                }

                ProductDTO? product = await response.Content.ReadFromJsonAsync<ProductDTO>();

                if (product == null)
                {
                    throw new ArgumentException("Invalid ProductID");
                }

                _logger.LogInformation("Product found in Product Microservice: {productID}, {productName}, {category}, {unitPrice}, {quantityInStock}", product.ProductID, product.ProductName, product.Category, product.UnitPrice, product.QuantityInStock);

                // Key: product:{productID}
                //Value: { "ProductName": "..", .. }

                string productJson = JsonSerializer.Serialize(product);

                DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(300));

                await _distributedCache.SetStringAsync(cacheKey, productJson, options);
                _logger.LogInformation($"Product added to cache: {product.ProductID}, {product.ProductName}, {product.Category}, {product.UnitPrice}, {product.QuantityInStock}");

                return product;
            }
            catch (BulkheadRejectedException ex)
            {
                _logger.LogError(ex, "Bulkhead isolation blocks the request since the request queue is full");

                return new ProductDTO(
                    ProductID: Guid.Empty,
                    ProductName: "Temporarily Unavailable (bulkhead)",
                    Category: "Temporarily Unavailable (bulkhead)",
                    UnitPrice: 0,
                    QuantityInStock: 0);
            }
        }
    }
}