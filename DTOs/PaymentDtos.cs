namespace PinoyPantry.API.DTOs;

public class CreatePaymentIntentDto
{
    public List<CartItemDto> Items { get; set; } = new();
}

public class CartItemDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class PaymentIntentResponseDto
{
    public string ClientSecret { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}
