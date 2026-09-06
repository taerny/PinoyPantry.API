using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Services;

namespace PinoyPantry.API.Controllers;

[ApiController]
[Route("api/bank-details")]
public class BankDetailsController : ControllerBase
{
    private readonly IBankDetailsService _bankDetailsService;

    public BankDetailsController(IBankDetailsService bankDetailsService)
    {
        _bankDetailsService = bankDetailsService;
    }

    // GET: api/bank-details — public, used on checkout/invoices for payment instructions
    [HttpGet]
    public async Task<ActionResult<BankDetailsDto>> GetBankDetails()
    {
        return Ok(await _bankDetailsService.GetBankDetailsAsync());
    }

    // PUT: api/bank-details — Admin only
    [Authorize(Roles = "Admin")]
    [HttpPut]
    public async Task<ActionResult<BankDetailsDto>> UpdateBankDetails(BankDetailsDto dto)
    {
        return Ok(await _bankDetailsService.UpdateBankDetailsAsync(dto));
    }
}
