using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;

using FluentValidation;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators;

public class OrderAddRequestValidator : AbstractValidator<OrderAddRequest>
{
    public OrderAddRequestValidator()
    {
        // UserID
        RuleFor(model => model.UserID)
            .NotEmpty()
            .WithMessage("UserID cannot be empty.");

        // OrderDate
        RuleFor(model => model.OrderDate)
            .NotEmpty()
            .WithMessage("OrderDate cannot be empty.");

        // OrderItems
        RuleFor(model => model.OrderItems)
            .NotEmpty()
            .WithMessage("OrderItems cannot be empty.")
            .Must(items => items.Count > 0)
            .WithMessage("OrderItems must contain at least one item.");
    }
}