using Microsoft.Extensions.Hosting;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ;

public class RabbitMQProductUpdateHostedService : IHostedService
{
    private readonly IRabbitMQProductUpdateConsumer _rabbitMQProductNameUpdateConsumer;

    public RabbitMQProductUpdateHostedService(IRabbitMQProductUpdateConsumer rabbitMQProductNameUpdateConsumer)
    {
        _rabbitMQProductNameUpdateConsumer = rabbitMQProductNameUpdateConsumer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _rabbitMQProductNameUpdateConsumer.InitializeAsync();
        await _rabbitMQProductNameUpdateConsumer.ConsumeAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _rabbitMQProductNameUpdateConsumer.DisposeAsync();
    }
}