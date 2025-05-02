using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;

using MongoDB.Driver;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;

public interface IOrdersService
{
    /// <summary>
    /// Gets the list of orders from the orders repository.
    /// </summary>
    /// <returns>The list of orders.</returns>
    Task<List<OrderResponse?>> GetOrders();

    /// <summary>
    /// Get a list of orders by condition.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    /// <returns>A list of orders that match the filter.</returns>
    Task<List<OrderResponse?>> GetOrdersByCondition(FilterDefinition<Order> filter);

    /// <summary>
    /// Get a single order by condition.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    /// <returns>An order that matches the filter, or null if not found.</returns>
    Task<OrderResponse?> GetOrderByCondition(FilterDefinition<Order> filter);

    /// <summary>
    /// Add a new order to the database.
    /// </summary>
    /// <param name="orderAddRequest">The order to add.</param>
    /// <returns>The added order, or null if the operation failed.</returns>
    Task<OrderResponse?> AddOrder(OrderAddRequest orderAddRequest);

    /// <summary>
    /// Update an existing order in the database.
    /// </summary>
    /// <param name="orderUpdateRequest">Update request containing the order details.</param>
    /// <returns>The updated order, or null if the operation failed.</returns>
    Task<OrderResponse?> UpdateOrder(OrderUpdateRequest orderUpdateRequest);

    /// <summary>
    /// Delete an order from the database.
    /// </summary>
    /// <param name="orderId">The ID of the order to delete.</param>
    /// <returns>True if the order was deleted successfully, false otherwise.</returns>
    Task<bool> DeleteOrder(Guid orderId);
}