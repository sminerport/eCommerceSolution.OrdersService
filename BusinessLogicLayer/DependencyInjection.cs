using eCommerce.OrdersMicroservice.BusinessLogicLayer.Mappers;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.RabbitMQ;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Services;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators;

using FluentValidation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogicLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssemblyContaining<OrderAddRequestValidator>();
        services.AddAutoMapper(typeof(OrderAddRequestToOrderMappingProfile).Assembly);
        services.AddScoped<IOrdersService, OrdersService>();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = $"{configuration["REDIS_HOST"]}:{configuration["REDIS_PORT"]}";
        });
        services.AddSingleton<IRabbitMQProductUpdateConsumer, RabbitMQProductUpdateConsumer>();
        services.AddSingleton<IRabbitMQProductDeleteConsumer, RabbitMQProductDeleteConsumer>();
        services.AddHostedService<RabbitMQProductUpdateHostedService>();
        services.AddHostedService<RabbitMQProductDeleteHostedService>();
        return services;
    }
}