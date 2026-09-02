using PinoyPantry.API.DTOs;

namespace PinoyPantry.API.Services
{
    public interface IOrderService
    {
        Task<List<OrderResponseDto>> GetAllOrdersAsync();
        Task<OrderResponseDto?> GetOrderByIdAsync(int id);
        Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto);
        Task<(OrderResponseDto? Order, string? Error)> UpdateStatusAsync(int id, string status);
        Task<(OrderResponseDto? Order, string? Error)> UpdateDeliveryFeeAsync(int id, decimal deliveryFee);
    }
}
