using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinoyPantry.API.Data;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HeroContentController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public HeroContentController(ApplicationDBContext context)
        {
            _context = context;
        }

        // GET: api/herocontent — public, powers the homepage hero section
        [HttpGet]
        public async Task<ActionResult<HeroContentDto>> GetHeroContent()
        {
            var hero = await _context.HeroContents.FirstOrDefaultAsync();
            if (hero == null)
                return NotFound(new { message = "Hero content has not been configured yet." });

            return Ok(new HeroContentDto
            {
                Headline = hero.Headline,
                HighlightedText = hero.HighlightedText,
                Subtext = hero.Subtext,
                ButtonText = hero.ButtonText,
                ButtonLink = hero.ButtonLink,
                FooterAboutText = hero.FooterAboutText,
                TopBarText = hero.TopBarText,
                IsMaintenanceMode = hero.IsMaintenanceMode,
                MaintenanceHeadline = hero.MaintenanceHeadline,
                MaintenanceMessage = hero.MaintenanceMessage
            });
        }

        // PUT: api/herocontent — Admin only
        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<ActionResult<HeroContentDto>> UpdateHeroContent(HeroContentDto dto)
        {
            var hero = await _context.HeroContents.FirstOrDefaultAsync();
            if (hero == null)
            {
                hero = new HeroContent();
                _context.HeroContents.Add(hero);
            }

            hero.Headline = dto.Headline;
            hero.HighlightedText = dto.HighlightedText;
            hero.Subtext = dto.Subtext;
            hero.ButtonText = dto.ButtonText;
            hero.ButtonLink = dto.ButtonLink;
            hero.FooterAboutText = dto.FooterAboutText;
            hero.TopBarText = dto.TopBarText;
            hero.IsMaintenanceMode = dto.IsMaintenanceMode;
            hero.MaintenanceHeadline = dto.MaintenanceHeadline;
            hero.MaintenanceMessage = dto.MaintenanceMessage;
            hero.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(dto);
        }
    }
}
