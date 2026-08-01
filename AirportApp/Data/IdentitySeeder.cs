using Microsoft.AspNetCore.Identity;

namespace AirportApp.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // 1. Crear roles mínimos (Administrador y Cliente)
        string[] roles = { "Administrador", "Cliente" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Crear cuenta de Administrador por defecto
        string emailAdmin = "admin@gmail.com";
        string passwordAdmin = "Admin123!";

        if (await userManager.FindByEmailAsync(emailAdmin) == null)
        {
            var admin = new IdentityUser 
            { 
                UserName = emailAdmin, 
                Email = emailAdmin,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, passwordAdmin);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Administrador");
            }
        }
    }
}
