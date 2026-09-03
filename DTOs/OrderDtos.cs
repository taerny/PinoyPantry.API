namespace PinoyPantry.API.DTOs
{
    public class CreateOrderDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string? Notes { get; set; }
        public string DeliveryMethod { get; set; } = string.Empty;
        public List<OrderItemRequestDto> Items { get; set; } = new();
    }

    public class OrderItemRequestDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    // Admin-only: records an in-person cash sale (e.g. a friend buying directly from the
    // owner). Already paid, so no delivery method / bank transfer flow applies. Customer
    // details are optional — some walk-in customers won't want to give any.
    public class CreateWalkInOrderDto
    {
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? Notes { get; set; }
        // True (default) = cash already in hand, order starts as Paid. False = "pay me
        // later" on trust — starts as Pending, same as an online order, so the owner can
        // mark it Paid via the usual button once the friend actually pays.
        public bool AlreadyPaid { get; set; } = true;
        public List<OrderItemRequestDto> Items { get; set; } = new();
    }

    public class OrderResponseDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string? Notes { get; set; }
        public string? DeliveryMethod { get; set; }
        public decimal? DeliveryFee { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Channel { get; set; } = "Online";
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }

    public class UpdateDeliveryFeeDto
    {
        public decimal DeliveryFee { get; set; }
    }

    public class OrderItemResponseDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
