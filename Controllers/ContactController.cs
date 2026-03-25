using Microsoft.AspNetCore.Mvc;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Services;

namespace PinoyPantry.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactController> _logger;

    public ContactController(IEmailService emailService, ILogger<ContactController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ContactRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Subject) ||
            string.IsNullOrWhiteSpace(dto.Message))
        {
            return BadRequest(new { message = "All fields are required." });
        }

        try
        {
            await _emailService.SendContactEmailAsync(dto);
            _logger.LogInformation("Contact email sent from {Email}", dto.Email);
            return Ok(new { message = "Message sent successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact email from {Email}", dto.Email);
            return StatusCode(500, new { message = "Failed to send message. Please try again later." });
        }
    }
}
