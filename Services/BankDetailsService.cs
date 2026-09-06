using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Data;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Services;

public class BankDetailsService : IBankDetailsService
{
    private readonly ApplicationDBContext _context;
    private readonly IConfiguration _configuration;

    public BankDetailsService(ApplicationDBContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // The DB row is the source of truth once an admin has set it. Until then, fall back
    // to the appsettings/env-var values so nothing breaks before the first save.
    public async Task<BankDetailsDto> GetBankDetailsAsync()
    {
        var row = await _context.BankDetails.FirstOrDefaultAsync();
        var section = _configuration.GetSection("BankTransfer");

        return new BankDetailsDto
        {
            BankName = !string.IsNullOrWhiteSpace(row?.BankName) ? row.BankName : section["BankName"] ?? "",
            AccountName = !string.IsNullOrWhiteSpace(row?.AccountName) ? row.AccountName : section["AccountName"] ?? "",
            AccountNumber = !string.IsNullOrWhiteSpace(row?.AccountNumber) ? row.AccountNumber : section["AccountNumber"] ?? "",
        };
    }

    public async Task<BankDetailsDto> UpdateBankDetailsAsync(BankDetailsDto dto)
    {
        var row = await _context.BankDetails.FirstOrDefaultAsync();
        if (row == null)
        {
            row = new BankDetails();
            _context.BankDetails.Add(row);
        }

        row.BankName = dto.BankName;
        row.AccountName = dto.AccountName;
        row.AccountNumber = dto.AccountNumber;
        row.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return dto;
    }
}
