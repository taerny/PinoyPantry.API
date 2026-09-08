using FluentValidation;
using PinoyPantry.API.DTOs;

namespace PinoyPantry.API.Validators
{
    public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
    {
        public CreateProductDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(100).WithMessage("Product name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

            // A brand-new product has no batch yet, so there's no cost to base a Recommended
            // Retail on — forcing a real price here just means the admin types a throwaway
            // number that gets overwritten the moment the first batch sets a real cost. Price
            // can start at 0; it stays unpublished/out-of-stock until a batch brings it to life.
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required.");
        }
    }
}
