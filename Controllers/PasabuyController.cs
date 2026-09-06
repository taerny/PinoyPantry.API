using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Data;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;
using PinoyPantry.API.Services;

namespace PinoyPantry.API.Controllers;

[ApiController]
[Route("api/pasabuy")]
public class PasabuyController : ControllerBase
{
    private readonly ApplicationDBContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<PasabuyController> _logger;

    public PasabuyController(ApplicationDBContext context, IEmailService emailService, ILogger<PasabuyController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    // POST: api/pasabuy — public, submitted from the homepage Pasabuy section
    [HttpPost]
    public async Task<IActionResult> Create(CreatePasabuyOrderDto dto)
    {
        var name = dto.Name?.Trim() ?? "";
        var phone = dto.Phone?.Trim() ?? "";
        var items = dto.ItemsRequested?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(items))
            return BadRequest(new { message = "Name, phone, and what you'd like to order are required." });

        var order = new PasabuyOrder
        {
            Name = name,
            Phone = phone,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            ItemsRequested = items,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
        };

        _context.PasabuyOrders.Add(order);
        await _context.SaveChangesAsync();

        // Best-effort — a failed notification email shouldn't fail the customer's submission.
        try
        {
            await _emailService.SendNewPasabuyOrderNotificationAsync(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Pasabuy order notification email for order {OrderId}", order.Id);
        }

        return Ok(new { message = "Order request received! We'll be in touch to confirm." });
    }

    // GET: api/pasabuy — Admin only
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<PasabuyOrderResponseDto>>> GetAll()
    {
        var orders = await _context.PasabuyOrders
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new PasabuyOrderResponseDto
            {
                Id = o.Id,
                Name = o.Name,
                Phone = o.Phone,
                Email = o.Email,
                ItemsRequested = o.ItemsRequested,
                Notes = o.Notes,
                Contacted = o.Contacted,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return Ok(orders);
    }

    // PATCH: api/pasabuy/{id}/contacted — Admin only, toggles the follow-up flag
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/contacted")]
    public async Task<IActionResult> SetContacted(int id, SetPasabuyContactedDto dto)
    {
        var order = await _context.PasabuyOrders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "Order not found." });

        order.Contacted = dto.Contacted;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Updated." });
    }
}
