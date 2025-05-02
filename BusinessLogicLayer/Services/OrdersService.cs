using AutoMapper;

using BusinessLogicLayer.HttpClients;

using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;
using eCommerce.OrdersMicroservice.DataAccessLayer.RepositoryContracts;

using FluentValidation;

using MongoDB.Driver;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Services;

public class OrdersService : IOrdersService
{
    private readonly IOrdersRepository _ordersRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<OrderAddRequest> _orderAddRequestValidator;
    private readonly IValidator<OrderItemAddRequest> _orderItemAddRequestValidator;
    private readonly IValidator<OrderUpdateRequest> _orderUpdateRequestValidator;
    private readonly IValidator<OrderItemUpdateRequest> _orderItemUpdateRequestValidator;
    private readonly UsersMicroserviceClient _usersMicroserviceClient;
    private readonly ProductMicroserviceClient _productsMicroserviceClient;

    public OrdersService(
        IOrdersRepository ordersRepository,
        IMapper mapper,
        IValidator<OrderAddRequest> orderAddRequestValidator,
        IValidator<OrderItemAddRequest> orderItemAddRequestValidator,
        IValidator<OrderUpdateRequest> orderUpdateRequestValidator,
        IValidator<OrderItemUpdateRequest> orderItemUpdateRequestValidator,
        UsersMicroserviceClient usersMicroserviceClient,
        ProductMicroserviceClient productsMicroserviceClient)
    {
        _ordersRepository = ordersRepository;
        _mapper = mapper;
        _orderAddRequestValidator = orderAddRequestValidator;
        _orderItemAddRequestValidator = orderItemAddRequestValidator;
        _orderUpdateRequestValidator = orderUpdateRequestValidator;
        _orderItemUpdateRequestValidator = orderItemUpdateRequestValidator;
        _usersMicroserviceClient = usersMicroserviceClient;
        _productsMicroserviceClient = productsMicroserviceClient;
    }

    public async Task<OrderResponse?> AddOrder(OrderAddRequest orderAddRequest)
    {
        if (orderAddRequest == null)
            throw new ArgumentNullException(nameof(orderAddRequest));

        // 1) Validate the request
        var validation = _orderAddRequestValidator.Validate(orderAddRequest);
        if (!validation.IsValid)
            throw new ArgumentException(
                "Validation failed: " +
                string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))
            );

        // 2) Load all ProductDTOs & validate each
        var products = new List<ProductDTO>();
        foreach (var itemReq in orderAddRequest.OrderItems)
        {
            var itemValidation = await _orderItemAddRequestValidator.ValidateAsync(itemReq);
            if (!itemValidation.IsValid)
                throw new ArgumentException(
                    "Validation failed: " +
                    string.Join(", ", itemValidation.Errors.Select(e => e.ErrorMessage))
                );

            var product = await _productsMicroserviceClient.GetProductByProductID(itemReq.ProductID);
            if (product == null)
                throw new ArgumentException($"Invalid ProductID: {itemReq.ProductID}");

            products.Add(product);
        }

        // 3) Load the user
        var userDTO = await _usersMicroserviceClient.GetUserByUserID(orderAddRequest.UserID);
        if (userDTO == null)
            throw new ArgumentException($"Invalid UserID: {orderAddRequest.UserID}");

        // 4) Map the add‐request → Order entity and compute totals
        var orderEntity = _mapper.Map<Order>(orderAddRequest);
        for (int i = 0; i < orderEntity.OrderItems.Count; i++)
        {
            var unitPrice = Convert.ToDecimal(products[i].UnitPrice);
            orderEntity.OrderItems[i].TotalPrice = unitPrice * orderEntity.OrderItems[i].Quantity;
        }
        orderEntity.TotalBill = orderEntity.OrderItems.Sum(x => x.TotalPrice);

        // 5) Persist
        var created = await _ordersRepository.AddOrder(orderEntity);
        if (created == null)
            return null;

        // 6) Map back to OrderResponse
        var response = _mapper.Map<OrderResponse>(created);

        // 7) Rebuild the immutable OrderItems list with product info + recalculated totals
        if (response.OrderItems != null && response.OrderItems.Count > 0)
        {
            var rebuilt = response.OrderItems
                .Select(oldItem =>
                {
                    // find the matching ProductDTO we fetched earlier
                    var prod = products.First(p => p.ProductID == oldItem.ProductID);

                    return oldItem with
                    {
                        ProductName = prod.ProductName,
                        Category = prod.Category,
                        UnitPrice = Convert.ToDecimal(prod.UnitPrice),
                        TotalPrice = Convert.ToDecimal(prod.UnitPrice) * oldItem.Quantity
                    };
                })
                .ToList();

            // 8) clone the parent response with the new list
            response = response with { OrderItems = rebuilt };
        }

        // 9) Map in user info
        _mapper.Map(userDTO, response);

        return response;
    }

    public async Task<OrderResponse?> UpdateOrder(OrderUpdateRequest orderUpdateRequest)
    {
        if (orderUpdateRequest == null)
            throw new ArgumentNullException(nameof(orderUpdateRequest));

        // 1) Validate
        var validation = _orderUpdateRequestValidator.Validate(orderUpdateRequest);
        if (!validation.IsValid)
            throw new ArgumentException(
                "Validation failed: " +
                string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))
            );

        // 2) Load products & validate
        var products = new List<ProductDTO>();
        foreach (var itemReq in orderUpdateRequest.OrderItems)
        {
            var itemValidation = await _orderItemUpdateRequestValidator.ValidateAsync(itemReq);
            if (!itemValidation.IsValid)
                throw new ArgumentException(
                    "Validation failed: " +
                    string.Join(", ", itemValidation.Errors.Select(e => e.ErrorMessage))
                );

            var product = await _productsMicroserviceClient.GetProductByProductID(itemReq.ProductID);
            if (product == null)
                throw new ArgumentException($"Invalid ProductID: {itemReq.ProductID}");

            products.Add(product);
        }

        // 3) Load user
        var userDTO = await _usersMicroserviceClient.GetUserByUserID(orderUpdateRequest.UserID);
        if (userDTO == null)
            throw new ArgumentException($"Invalid UserID: {orderUpdateRequest.UserID}");

        // 4) Map update request → Order entity + recalc totals
        var orderEntity = _mapper.Map<Order>(orderUpdateRequest);
        for (int i = 0; i < orderEntity.OrderItems.Count; i++)
        {
            var unitPrice = Convert.ToDecimal(products[i].UnitPrice);
            orderEntity.OrderItems[i].TotalPrice = unitPrice * orderEntity.OrderItems[i].Quantity;
        }
        orderEntity.TotalBill = Convert.ToDecimal(orderEntity.OrderItems.Sum(x => x.TotalPrice));

        // 5) Persist
        var updated = await _ordersRepository.UpdateOrder(orderEntity);
        if (updated == null)
            return null;

        // 6) Map back to OrderResponse
        var response = _mapper.Map<OrderResponse>(updated);

        // 7) Rebuild immutable OrderItems list
        if (response.OrderItems != null && response.OrderItems.Count > 0)
        {
            var rebuilt = response.OrderItems
                .Select(oldItem =>
                {
                    var prod = products.First(p => p.ProductID == oldItem.ProductID);
                    return oldItem with
                    {
                        ProductName = prod.ProductName,
                        Category = prod.Category,
                        UnitPrice = Convert.ToDecimal(prod.UnitPrice),
                        TotalPrice = Convert.ToDecimal(prod.UnitPrice) * oldItem.Quantity
                    };
                })
                .ToList();

            response = response with { OrderItems = rebuilt };
        }

        // 8) Map in user info
        _mapper.Map(userDTO, response);

        return response;
    }

    public async Task<OrderResponse?> GetOrderByCondition(FilterDefinition<Order> filter)
    {
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        // Order does not contain the product details, so we retrieve them later
        Order? existingOrder = await _ordersRepository.GetOrderByCondition(filter);

        if (existingOrder == null)
        {
            return null;
        }

        OrderResponse orderResponse = _mapper.Map<OrderResponse>(existingOrder);

        if (orderResponse != null)
        {
            if (orderResponse.OrderItems == null || orderResponse.OrderItems.Count == 0)
            {
                return orderResponse;
            }

            var updatedItems = new List<OrderItemResponse>(orderResponse.OrderItems.Count);
            foreach (OrderItemResponse oldItem in orderResponse.OrderItems)
            {
                // Get the product details from the products microservice
                // If the response is not in the cache, it will be retrieved from the database
                // If the response is in the cache, it will be retrieved from the cache
                ProductDTO? productDTO = await _productsMicroserviceClient.GetProductByProductID(oldItem.ProductID);

                if (productDTO == null)
                {
                    updatedItems.Add(oldItem);
                    continue;
                }

                var newItem = oldItem with
                {
                    ProductName = productDTO.ProductName,
                    Category = productDTO.Category,
                    UnitPrice = Convert.ToDecimal(productDTO.UnitPrice),
                    TotalPrice = Convert.ToDecimal(productDTO.UnitPrice) * oldItem.Quantity
                };

                updatedItems.Add(newItem);
            }

            orderResponse = orderResponse with { OrderItems = updatedItems, TotalBill = updatedItems.Sum(x => x.TotalPrice) };

            UserDTO? userDTO = await _usersMicroserviceClient.GetUserByUserID(orderResponse.UserID);

            if (userDTO != null)
            {
                _mapper.Map<UserDTO, OrderResponse>(userDTO, orderResponse);
            }
        }

        return orderResponse;
    }

    public async Task<List<OrderResponse?>> GetOrders()
    {
        IEnumerable<Order?> ordersFromDatabase = await _ordersRepository.GetOrders();

        if (ordersFromDatabase == null)
        {
            return new List<OrderResponse?>();
        }

        IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse>>(ordersFromDatabase);

        var newResponses = await MapProductsToOrderItemResponses(orderResponses);

        return newResponses.ToList();
    }

    public async Task<List<OrderResponse?>> GetOrdersByCondition(FilterDefinition<Order> filter)
    {
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        IEnumerable<Order?> ordersFromDatabase = await _ordersRepository.GetOrdersByCondition(filter);

        IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse>>(ordersFromDatabase);

        var newResponses = await MapProductsToOrderItemResponses(orderResponses);

        return newResponses.ToList();
    }

    public async Task<bool> DeleteOrder(Guid orderID)
    {
        FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderID, orderID);
        Order? existingOrder = await _ordersRepository.GetOrderByCondition(filter);

        if (existingOrder == null)
        {
            return false;
        }

        bool isDeleted = await _ordersRepository.DeleteOrder(orderID);

        return isDeleted;
    }

    private async Task<List<OrderResponse?>> MapProductsToOrderItemResponses(
        IEnumerable<OrderResponse?> orderResponses)
    {
        // 1) initialize your result list
        var newResponses = new List<OrderResponse?>();

        foreach (var orderResponse in orderResponses)
        {
            // 2) if the response is null or has no items, just carry it forward as-is
            if (orderResponse?.OrderItems is not { Count: > 0 } oldItems)
            {
                newResponses.Add(orderResponse);
                continue;
            }

            // 3) rebuild the OrderItems list, cloning each item with updated price/info
            var updatedItems = new List<OrderItemResponse>(oldItems.Count);
            foreach (var oldItem in oldItems)
            {
                var productDTO = await _productsMicroserviceClient
                                         .GetProductByProductID(oldItem.ProductID);
                if (productDTO is null)
                {
                    updatedItems.Add(oldItem);
                    continue;
                }

                var newItem = oldItem with
                {
                    ProductName = productDTO.ProductName,
                    Category = productDTO.Category,
                    UnitPrice = Convert.ToDecimal(productDTO.UnitPrice),
                    TotalPrice = Convert.ToDecimal(productDTO.UnitPrice) * oldItem.Quantity
                };

                updatedItems.Add(newItem);
            }

            // Compute the new TotalBill as the sum of updated line totals
            decimal newTotalBill = updatedItems.Sum(item => item.TotalPrice);

            // Clone the OrderResponse, setting both OrderItems and TotalBill
            var newOrder = orderResponse with
            {
                OrderItems = updatedItems,
                TotalBill = newTotalBill
            };

            // 5) map in the UserDTO on that newOrder
            var userDTO = await _usersMicroserviceClient.GetUserByUserID(newOrder.UserID);
            if (userDTO is not null)
            {
                _mapper.Map(userDTO, newOrder);
            }

            // 6) add it to your results
            newResponses.Add(newOrder);
        }

        return newResponses;
    }
}