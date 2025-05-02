using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

using FluentValidation;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators;

public class OrderItemUpdateRequestValidator : AbstractValidator<OrderItemUpdateRequest>
{
    public OrderItemUpdateRequestValidator()
    {
        // ProductID
        RuleFor(model => model.ProductID)
            .NotEmpty()
            .WithMessage("ProductID cannot be empty.");

        // UnitPrice
        RuleFor(model => model.UnitPrice)
            .NotEmpty()
            .WithMessage("UnitPrice cannot be empty.")
            .GreaterThan(0)
            .WithMessage("UnitPrice must be greater than zero.");

        // Quantity
        RuleFor(model => model.Quantity)
            .NotEmpty()
            .WithMessage("Quantity cannot be empty.")
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
    }
}