using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;

using MongoDB.Driver;

namespace eCommerce.OrdersMicroservice.DataAccessLayer.RepositoryContracts;

public interface IOrdersRepository
{
    /// <summary>
    /// Get all orders from the database.
    /// </summary>
    /// <returns>Returns a list of orders.</returns>
    Task<IEnumerable<Order>> GetOrders();

    /// <summary>
    /// Get orders by condition.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    /// <returns>An enumerable of orders that match the filter.</returns>
    Task<IEnumerable<Order?>> GetOrdersByCondition(FilterDefinition<Order> filter);

    /// <summary>
    /// Get a single order by condition.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    /// <returns>An order that matches the filter, or null if not found.</returns>
    Task<Order?> GetOrderByCondition(FilterDefinition<Order> filter);

    /// <summary>
    /// Add a new order to the database.
    /// </summary>
    /// <param name="order">The order to add.</param>
    /// <returns>The added order, or null if the operation failed.</returns>
    Task<Order?> AddOrder(Order order);

    /// <summary>
    /// Update an existing order in the database.
    /// </summary>
    /// <param name="order">The order to update.</param>
    /// <returns>The updated order, or null if the operation failed.</returns>
    Task<Order?> UpdateOrder(Order order);

    /// <summary>
    /// Delete an order from the database.
    /// </summary>
    /// <param name="orderId">The ID of the order to delete.</param>
    /// <returns>True if the order was deleted successfully, false otherwise.</returns>
    Task<bool> DeleteOrder(Guid orderId);
}