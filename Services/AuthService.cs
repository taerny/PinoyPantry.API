using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PinoyPantry.API.Data;
using PinoyPantry.API.DTOs;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDBContext _context;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ApplicationDBContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _context = context;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            PhoneNumber = dto.Phone,
            Address = dto.Address,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }

        await _userManager.AddToRoleAsync(user, "Customer");
        return await GenerateTokenResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            throw new UnauthorizedAccessException("Invalid email or password.");

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await GenerateTokenResponse(user);
    }

    public async Task<UserProfileDto?> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserProfileDto
        {
            Email = user.Email!,
            FullName = user.FullName,
            Phone = user.PhoneNumber ?? "",
            Address = user.Address,
            Role = roles.FirstOrDefault() ?? "Customer",
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var products = await _context.Products.ToListAsync();
        var userCount = await _context.Users.CountAsync();

        var categoryStats = products
            .GroupBy(p => p.Category)
            .Select(g => new CategoryStatDto { Category = g.Key, Count = g.Count() })
            .OrderByDescending(c => c.Count)
            .ToList();

        var recentProducts = products
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Category = p.Category,
                Price = p.Price,
                HasImage = !string.IsNullOrEmpty(p.ImageUrl)
            })
            .ToList();

        return new DashboardStatsDto
        {
            TotalProducts = products.Count,
            TotalUsers = userCount,
            ProductsWithImages = products.Count(p => !string.IsNullOrEmpty(p.ImageUrl)),
            TotalCategories = categoryStats.Count,
            TotalInventoryValue = products.Sum(p => p.Price * p.StockQuantity),
            CategoryStats = categoryStats,
            RecentProducts = recentProducts
        };
    }

    private async Task<AuthResponseDto> GenerateTokenResponse(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Customer";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var hours = int.Parse(_configuration["Jwt:ExpirationInHours"] ?? "24");
        var expiration = DateTime.UtcNow.AddHours(hours);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: creds
        );

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email!,
            FullName = user.FullName,
            Role = role,
            Expiration = expiration
        };
    }
}
