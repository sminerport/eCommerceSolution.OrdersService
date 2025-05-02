
namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ
{
    public interface IRabbitMQProductUpdateConsumer
    {
        Task ConsumeAsync();
        ValueTask DisposeAsync();
        Task InitializeAsync();
    }
}