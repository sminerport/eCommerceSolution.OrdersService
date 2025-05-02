
namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ
{
    public interface IRabbitMQProductDeleteConsumer
    {
        Task ConsumeAsync();
        ValueTask DisposeAsync();
        Task InitializeAsync();
    }
}