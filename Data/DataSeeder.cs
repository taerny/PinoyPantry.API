using Microsoft.AspNetCore.Identity;
using PinoyPantry.API.Models;

namespace PinoyPantry.API.Data;

public static class DataSeeder
{
    public static async Task SeedRolesAndAdmin(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = { "Admin", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Only bootstrap a default admin on a database that has none at all. If an admin already
        // exists under any email (e.g. the account was renamed), never create a second one with
        // the well-known default password.
        var adminEmail = "admin@pinoypantry.co.nz";
        if ((await userManager.GetUsersInRoleAsync("Admin")).Count == 0
            && await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "PinoyPantry Admin",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}
