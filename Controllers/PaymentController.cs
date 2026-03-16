using Microsoft.AspNetCore.Mvc;
using PinoyPantry.API.DTOs;
using Stripe;

namespace PinoyPantry.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public PaymentController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("create-payment-intent")]
    public async Task<IActionResult> CreatePaymentIntent(CreatePaymentIntentDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            return BadRequest(new { message = "Cart is empty." });

        var totalAmount = dto.Items.Sum(item => item.Price * item.Quantity);
        var amountInCents = (long)(totalAmount * 100);

        if (amountInCents < 50)
            return BadRequest(new { message = "Minimum order amount is $0.50." });

        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInCents,
            Currency = "nzd",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
            Metadata = new Dictionary<string, string>
            {
                { "item_count", dto.Items.Count.ToString() },
                { "total", totalAmount.ToString("F2") }
            }
        };

        var service = new PaymentIntentService();
        var paymentIntent = await service.CreateAsync(options);

        return Ok(new PaymentIntentResponseDto
        {
            ClientSecret = paymentIntent.ClientSecret,
            Amount = amountInCents,
            Currency = "nzd"
        });
    }

    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        return Ok(new { publishableKey = _configuration["Stripe:PublishableKey"] });
    }
}
