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
    private readonly IBlobStorageService _blobService;
    private readonly ILogger<PasabuyController> _logger;

    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };
    private const long MaxImageSize = 5 * 1024 * 1024; // 5 MB — same limit as the admin product image upload

    public PasabuyController(ApplicationDBContext context, IEmailService emailService, IBlobStorageService blobService, ILogger<PasabuyController> logger)
    {
        _context = context;
        _emailService = emailService;
        _blobService = blobService;
        _logger = logger;
    }

    // POST: api/pasabuy/upload-item-image — public (no login), unlike the admin image upload
    // endpoint — a Pasabuy customer submitting a reference photo isn't authenticated. Same
    // file-type/size restrictions as the admin upload keep this from being an open dumping
    // ground; it just doesn't get attached to a Product.
    [HttpPost("upload-item-image")]
    public async Task<IActionResult> UploadItemImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        if (file.Length > MaxImageSize)
            return BadRequest(new { message = "File size exceeds 5 MB limit." });

        if (!AllowedImageTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Only JPEG, PNG, WebP, and GIF images are allowed." });

        using var stream = file.OpenReadStream();
        var imageUrl = await _blobService.UploadImageAsync(stream, file.FileName, file.ContentType);

        return Ok(new { imageUrl });
    }

    // POST: api/pasabuy — public, submitted from the homepage Pasabuy section
    [HttpPost]
    public async Task<IActionResult> Create(CreatePasabuyOrderDto dto)
    {
        var name = dto.Name?.Trim() ?? "";
        var phone = dto.Phone?.Trim() ?? "";
        var items = (dto.Items ?? new())
            .Where(i => !string.IsNullOrWhiteSpace(i.ProductName))
            .Select(i => new PasabuyOrderItem
            {
                ProductName = i.ProductName.Trim(),
                Quantity = i.Quantity < 1 ? 1 : i.Quantity,
                ImageUrl = string.IsNullOrWhiteSpace(i.ImageUrl) ? null : i.ImageUrl.Trim(),
                Notes = string.IsNullOrWhiteSpace(i.Notes) ? null : i.Notes.Trim()
            })
            .ToList();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { message = "Name and phone are required." });

        if (items.Count == 0)
            return BadRequest(new { message = "Add at least one item you'd like to order." });

        var order = new PasabuyOrder
        {
            Name = name,
            Phone = phone,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            Items = items,
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
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new PasabuyOrderResponseDto
            {
                Id = o.Id,
                Name = o.Name,
                Phone = o.Phone,
                Email = o.Email,
                Items = o.Items.Select(i => new PasabuyOrderItemResponseDto
                {
                    Id = i.Id,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    ImageUrl = i.ImageUrl,
                    Notes = i.Notes
                }).ToList(),
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
