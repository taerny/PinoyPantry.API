using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Data;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Controllers;

[ApiController]
[Route("api/newsletter")]
public class NewsletterController : ControllerBase
{
    private readonly ApplicationDBContext _context;

    public NewsletterController(ApplicationDBContext context)
    {
        _context = context;
    }

    // POST: api/newsletter/subscribe — public, called from the homepage newsletter form
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe(NewsletterSubscribeDto dto)
    {
        var email = dto.Email?.Trim() ?? "";

        if (!IsValidEmail(email))
            return BadRequest(new { message = "Please enter a valid email address." });

        var alreadySubscribed = await _context.NewsletterSubscribers
            .AnyAsync(s => s.Email.ToLower() == email.ToLower());

        if (alreadySubscribed)
            return Ok(new { message = "You're already subscribed — thanks!" });

        _context.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = email });
        await _context.SaveChangesAsync();

        return Ok(new { message = "Subscribed! Thanks for staying connected." });
    }

    // GET: api/newsletter/subscribers — Admin only
    [Authorize(Roles = "Admin")]
    [HttpGet("subscribers")]
    public async Task<ActionResult<List<NewsletterSubscriberResponseDto>>> GetSubscribers()
    {
        var subscribers = await _context.NewsletterSubscribers
            .OrderByDescending(s => s.SubscribedAt)
            .Select(s => new NewsletterSubscriberResponseDto
            {
                Id = s.Id,
                Email = s.Email,
                SubscribedAt = s.SubscribedAt
            })
            .ToListAsync();

        return Ok(subscribers);
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            _ = new System.Net.Mail.MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
