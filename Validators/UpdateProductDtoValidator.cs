using FluentValidation;
using PinoyPantry.API.DTOs;

namespace PinoyPantry.API.Validators
{
    public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(100).WithMessage("Product name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

            // A product with no stock yet (no batch added) still needs to be editable — e.g.
            // setting its Margin before the first batch exists, so Recommended Retail (and
            // therefore Price) can auto-compute the moment that batch is added. Once real stock
            // exists, ProductService.UpdateProductAsync enforces Price > 0 itself.
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required.");
        }
    }
}
