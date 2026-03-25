using PinoyPantry.API.DTOs;

namespace PinoyPantry.API.Services;

public interface IEmailService
{
    Task SendContactEmailAsync(ContactRequestDto dto);
}
