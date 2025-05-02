namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

public record UserDTO(
    Guid UserID,
    string? PersonName,
    string? Email,
    string Gender);