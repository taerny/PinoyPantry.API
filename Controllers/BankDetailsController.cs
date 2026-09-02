using Microsoft.AspNetCore.Mvc;

namespace PinoyPantry.API.Controllers;

[ApiController]
[Route("api/bank-details")]
public class BankDetailsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public BankDetailsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // GET: api/bank-details — public, used on the invoice page for payment instructions
    [HttpGet]
    public IActionResult GetBankDetails()
    {
        var section = _configuration.GetSection("BankTransfer");
        return Ok(new
        {
            bankName = section["BankName"],
            accountName = section["AccountName"],
            accountNumber = section["AccountNumber"],
        });
    }
}
