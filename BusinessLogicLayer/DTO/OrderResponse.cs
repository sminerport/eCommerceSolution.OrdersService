namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public record OrderResponse(
    Guid OrderID,
    Guid UserID,
    string? UserPersonName,
    string? Email,
    DateTime OrderDate,
    decimal TotalBill,
    List<OrderItemResponse>? OrderItems)
{
    public OrderResponse() : this(
        default,
        default,
        default,
        default,
        default,
        default,
        default)
    {
    }
}