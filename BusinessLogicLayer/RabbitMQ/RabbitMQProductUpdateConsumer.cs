using System.Text;
using System.Text.Json;

using AutoMapper;

using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ;

public class RabbitMQProductUpdateConsumer : IAsyncDisposable, IRabbitMQProductUpdateConsumer
{
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;
    private ILogger<RabbitMQProductUpdateConsumer> _logger;
    private IDistributedCache _distributedCache;
    private IMapper _mapper;

    public RabbitMQProductUpdateConsumer(
        IConfiguration configuration,
        ILogger<RabbitMQProductUpdateConsumer> logger,
        IDistributedCache distributedCache,
        IMapper mapper)
    {
        _configuration = configuration;
        _logger = logger;
        _distributedCache = distributedCache;
        _mapper = mapper;
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
            { "event", "product.update" },
            { "row-count" , 1 }
        };

        string queueName = "orders.product.update.queue";

        if (_channel != null)
        {
            await _channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Headers,
                durable: true);

            // Create message queue
            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null); // x-message-ttl | x-max-length | x-expired

            // Bind the message to exchange
            await _channel.QueueBindAsync(
                queue: queueName,
                exchange: exchangeName,
                routingKey: string.Empty,
                arguments: headers);

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, args) =>
            {
                byte[] body = args.Body.ToArray();

                string message = Encoding.UTF8.GetString(body);

                if (message != null)
                {
                    ProductDTO? productDTO = JsonSerializer.Deserialize<ProductDTO>(message);

                    if (productDTO != null)
                    {
                        await HandleProductUpdationAsync(productDTO);
                    }
                }
            };
            await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: true,
                consumer: consumer);
        }
    }

    private async Task HandleProductUpdationAsync(ProductDTO productDTO)
    {
        string productJason = JsonSerializer.Serialize(productDTO);

        DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(300))
            .SetSlidingExpiration(TimeSpan.FromSeconds(100));

        await _distributedCache.SetStringAsync($"product:{productDTO.ProductID}", productJason, options);

        _logger.LogInformation($"Product added to cache: {productDTO.ProductID}, {productDTO.ProductName}, {productDTO.Category}, {productDTO.UnitPrice}, {productDTO.QuantityInStock}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_channel is not null)
        {
            // gracefully close the channel
            try
            { await _channel.CloseAsync(); }
            catch { /* TODO: log if needed */ }

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
            // gracefully close the connection
            try
            { await _connection.CloseAsync(); }
            catch { /* TODO: log if needed */ }
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