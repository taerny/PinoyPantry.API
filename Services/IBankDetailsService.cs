using PinoyPantry.API.DTOs;

namespace PinoyPantry.API.Services;

public interface IBankDetailsService
{
    Task<BankDetailsDto> GetBankDetailsAsync();
    Task<BankDetailsDto> UpdateBankDetailsAsync(BankDetailsDto dto);
}
