using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ;

public class RabbitMQProductDeleteConsumer : IAsyncDisposable, IRabbitMQProductDeleteConsumer
{
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;
    private ILogger<RabbitMQProductDeleteConsumer> _logger;
    private IDistributedCache _distributedCache;

    public RabbitMQProductDeleteConsumer(
        IConfiguration configuration,
        ILogger<RabbitMQProductDeleteConsumer> logger,
        IDistributedCache distributedCache)
    {
        _configuration = configuration;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task InitializeAsync()
    {
        ConnectionFactory connectionFactory = new ConnectionFactory
        {
            HostName = _configuration["RABBITMQ_HOST"]!,
            UserName = _configuration["RABBITMQ_USERNAME"]!,
            Password = _configuration["RABBITMQ_PASSWORD"]!,
            Port = Convert.ToInt32(_configuration["RABBITMQ_PORT"]!)
        };

        _connection = await connectionFactory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
    }

    public async Task ConsumeAsync()
    {
        string exchangeName = _configuration["RABBITMQ_PRODUCTS_EXCHANGE"]!;

        Dictionary<string, object?> headers = new Dictionary<string, object?>()
        {
            { "x-match", "all" },
            { "event", "product.delete" },
            { "row-count", 1 }
        };

        string queueName = "orders.product.delete.queue";

        if (_channel != null)
        {
            await _channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Headers,
                durable: true);

            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await _channel.QueueBindAsync(queueName, exchangeName, string.Empty, headers);

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, args) =>
            {
                byte[] body = args.Body.ToArray();

                string message = Encoding.UTF8.GetString(body);

                ProductDeleteMessage? productToDelete = JsonSerializer.Deserialize<ProductDeleteMessage>(message);

                if (productToDelete != null)
                {
                    await HandleProductDeletionAsync(productToDelete);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: true,
                consumer: consumer);
        }
    }

    private async Task HandleProductDeletionAsync(ProductDeleteMessage productDeletionMessage)
    {
        string cacheKey = $"product:{productDeletionMessage.ProductID}";

        await _distributedCache.RemoveAsync($"{cacheKey}");

        _logger.LogInformation($"Removed product from cache: {productDeletionMessage.ProductID}, {productDeletionMessage.ProductName}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_channel is not null)
        {
            try
            {
                await _channel.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error closing channel: {ex.Message}");
            }

            if (_channel is IAsyncDisposable asyncChannel)
            {
                await asyncChannel.DisposeAsync();
            }
            else
            {
                _channel.Dispose();
            }
        }

        if (_connection is not null)
        {
            try
            {
                await _connection.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error closing connection: {ex.Message}");
            }

            if (_connection is IAsyncDisposable asyncConnection)
            {
                await asyncConnection.DisposeAsync();
            }
            else
            {
                _connection.Dispose();
            }
        }
    }
}