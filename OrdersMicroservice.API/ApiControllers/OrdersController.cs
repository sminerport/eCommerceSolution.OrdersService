using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using MongoDB.Driver;

namespace eCommerce.OrdersMicroservice.API.ApiControllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly IOrdersService _ordersService;
    private readonly IValidator<OrderAddRequest> _orderAddRequestValidator;
    private readonly IValidator<OrderItemAddRequest> _orderItemAddRequestValidator;
    private readonly IValidator<OrderUpdateRequest> _orderUpdateRequestValidator;
    private readonly IValidator<OrderItemUpdateRequest> _orderItemUpdateRequestValidator;

    public OrdersController(
        IOrdersService ordersService,
        IValidator<OrderAddRequest> orderAddRequestValidator,
        IValidator<OrderItemAddRequest> orderItemAddRequestValidator,
        IValidator<OrderUpdateRequest> orderUpdateRequestValidator,
        IValidator<OrderItemUpdateRequest> orderItemUpdateRequestValidator)
    {
        _ordersService = ordersService;
        _orderAddRequestValidator = orderAddRequestValidator;
        _orderItemAddRequestValidator = orderItemAddRequestValidator;
        _orderUpdateRequestValidator = orderUpdateRequestValidator;
        _orderItemUpdateRequestValidator = orderItemUpdateRequestValidator;
    }

    [HttpGet()]
    public async Task<IEnumerable<OrderResponse?>> GetOrders()
    {
        List<OrderResponse?> orders = await _ordersService.GetOrders();

        if (orders == null || orders.Count == 0)
        {
            return Enumerable.Empty<OrderResponse?>();
        }

        return orders;
    }

    [HttpGet("search/orderid/{orderID}")]
    public async Task<OrderResponse?> GetOrderByOrderId(Guid orderID)
    {
        if (orderID == Guid.Empty)
        {
            return null;
        }

        FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(x => x.OrderID, orderID);

        OrderResponse? order = await _ordersService.GetOrderByCondition(filter);

        return order;
    }

    [HttpGet("search/productid/{productID}")]
    public async Task<IEnumerable<OrderResponse?>> GetOrdersByProductId(Guid productID)
    {
        if (productID == Guid.Empty)
        {
            return Enumerable.Empty<OrderResponse?>();
        }

        FilterDefinition<Order> filter = Builders<Order>.Filter.ElemMatch(
            x => x.OrderItems,
            Builders<OrderItem>.Filter.Eq(y => y.ProductID, productID));

        List<OrderResponse?> orders = await _ordersService.GetOrdersByCondition(filter);

        return orders;
    }

    [HttpGet("search/orderDate/{orderDate}")]
    public async Task<IEnumerable<OrderResponse?>> GetOrdersByOrderDate(DateTime orderDate)
    {
        if (orderDate == default)
        {
            return Enumerable.Empty<OrderResponse?>();
        }

        DateTime dayStart = orderDate.Date;
        DateTime nextDay = dayStart.AddDays(1);

        FilterDefinition<Order> filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Gte(o => o.OrderDate, dayStart),
            Builders<Order>.Filter.Lt(o => o.OrderDate, nextDay));

        List<OrderResponse?> orders = await _ordersService.GetOrdersByCondition(filter);

        return orders;
    }

    [HttpGet("search/userid/{userID}")]
    public async Task<IEnumerable<OrderResponse?>> GetOrderByUserID(Guid userID)
    {
        if (userID == Guid.Empty)
        {
            return Enumerable.Empty<OrderResponse?>();
        }

        FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(x => x.UserID, userID);
        List<OrderResponse?> orders = await _ordersService.GetOrdersByCondition(filter);
        return orders;
    }

    [HttpPost]
    public async Task<IActionResult> AddOrder([FromBody] OrderAddRequest orderAddRequest)
    {
        if (orderAddRequest == null)
        {
            return BadRequest("Order cannot be null.");
        }

        OrderResponse? addedResponse = await _ordersService.AddOrder(orderAddRequest);

        if (addedResponse == null)
        {
            return Problem(detail: "Failed to add order.", statusCode: 500);
        }

        return CreatedAtAction(nameof(GetOrderByOrderId), new { orderID = addedResponse.OrderID }, addedResponse);
    }

    [HttpPut("{orderID}")]
    public async Task<IActionResult> UpdateOrder(Guid orderID, [FromBody] OrderUpdateRequest orderUpdateRequest)
    {
        if (orderUpdateRequest == null)
        {
            return BadRequest("Order cannot be null.");
        }
        if (orderID != orderUpdateRequest.OrderID)
        {
            return BadRequest("Order ID in the URL does not match the Order ID in the request body.");
        }
        OrderResponse? updatedResponse = await _ordersService.UpdateOrder(orderUpdateRequest);
        if (updatedResponse == null)
        {
            return Problem("Failed to update order.", statusCode: 500);
        }
        return Ok(updatedResponse);
    }

    [HttpDelete("{orderID}")]
    public async Task<IActionResult> DeleteOrder(Guid orderID)
    {
        if (orderID == Guid.Empty)
        {
            return BadRequest("Order ID cannot be empty.");
        }

        bool isDeleted = await _ordersService.DeleteOrder(orderID);

        if (!isDeleted)
        {
            return Problem("Error in deleting order");
        }

        return Ok(isDeleted);
    }
}