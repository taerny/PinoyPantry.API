using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Data;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;
using PinoyPantry.API.Services;

namespace PinoyPantry.API.Controllers;

[ApiController]
[Route("api/pasabuy-settings")]
public class PasabuySettingsController : ControllerBase
{
    private readonly ApplicationDBContext _context;

    public PasabuySettingsController(ApplicationDBContext context)
    {
        _context = context;
    }

    // GET: api/pasabuy-settings — public, powers the homepage Pasabuy section
    [HttpGet]
    public async Task<ActionResult<PasabuySettingsDto>> Get()
    {
        var settings = await PasabuySettingsService.LoadAsync(_context);
        return Ok(PasabuySettingsService.ToDto(settings));
    }

    // PUT: api/pasabuy-settings — Admin only
    [Authorize(Roles = "Admin")]
    [HttpPut]
    public async Task<ActionResult<PasabuySettingsDto>> Update(PasabuySettingsDto dto)
    {
        if (!DateTime.TryParseExact(dto.CutoffDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var cutoff))
            return BadRequest(new { message = "Please choose a valid cut-off date." });

        if (string.IsNullOrWhiteSpace(dto.Heading) || string.IsNullOrWhiteSpace(dto.BadgeText))
            return BadRequest(new { message = "Badge and heading cannot be empty." });

        var settings = await _context.PasabuySettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new PasabuySettings();
            _context.PasabuySettings.Add(settings);
        }

        PasabuySettingsService.Apply(settings, dto, cutoff);
        await _context.SaveChangesAsync();

        return Ok(PasabuySettingsService.ToDto(settings));
    }
}
