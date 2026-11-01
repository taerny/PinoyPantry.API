using PinoyPantry.API.DTOs;

namespace PinoyPantry.API.Services
{
    public interface IOrderService
    {
        Task<List<OrderResponseDto>> GetAllOrdersAsync();
        Task<OrderResponseDto?> GetOrderByIdAsync(int id);
        Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, string? customerIp, string? userAgent);
        Task<OrderResponseDto> CreateWalkInOrderAsync(CreateWalkInOrderDto dto);
        Task<(OrderResponseDto? Order, string? Error)> UpdateStatusAsync(int id, string status);
        Task<(OrderResponseDto? Order, string? Error)> UpdateDeliveryFeeAsync(int id, decimal deliveryFee);
        Task<(OrderResponseDto? Order, string? Error)> UpdateItemQuantityAsync(int orderId, int itemId, int quantity);
        Task<(OrderResponseDto? Order, string? Error)> RemoveItemAsync(int orderId, int itemId);
    }
}
