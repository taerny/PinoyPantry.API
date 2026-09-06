using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Services;

public interface IEmailService
{
    Task SendContactEmailAsync(ContactRequestDto dto);
    Task SendOrderConfirmationEmailAsync(Order order);
    Task SendNewOrderNotificationEmailAsync(Order order);
    Task SendDeliveryFeeConfirmedEmailAsync(Order order);
    Task SendWalkInReceiptEmailAsync(Order order);
    Task SendWalkInOwnerNotificationEmailAsync(Order order);
}
