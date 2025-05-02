namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public record OrderItemResponse(
    Guid ProductID,
    string? ProductName,
    string? Category,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice)
{
    public OrderItemResponse() : this(
        default,
        default,
        default,
        default,
        default,
        default)
    {
    }
}