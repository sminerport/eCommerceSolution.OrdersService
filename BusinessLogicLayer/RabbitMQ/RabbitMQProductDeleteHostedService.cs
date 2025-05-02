using Microsoft.Extensions.Hosting;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ;

public class RabbitMQProductDeleteHostedService : IHostedService
{
    private readonly IRabbitMQProductDeleteConsumer _rabbitMQProductDeleteConsumer;

    public RabbitMQProductDeleteHostedService(IRabbitMQProductDeleteConsumer rabbitMQProductDeleteConsumer)
    {
        _rabbitMQProductDeleteConsumer = rabbitMQProductDeleteConsumer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _rabbitMQProductDeleteConsumer.InitializeAsync();
        await _rabbitMQProductDeleteConsumer.ConsumeAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _rabbitMQProductDeleteConsumer.DisposeAsync();
    }
}