using CurrencyConverter.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Infrastructure.Persistence.SeedData
{
    [ExcludeFromCodeCoverage]
    public class UsersSeed
    {

        public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration config)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = new[] { "Admin", "User" };

            // Ensure roles exist
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed admin users
            var adminUsers = new List<User>();
            var adminsSection = config.GetSection("Roles:Admin");

            foreach (var admin in adminsSection.GetChildren())
            {
                adminUsers.Add(new User
                {
                    Email = admin.GetValue<string>("Email"),
                    Password = admin.GetValue<string>("Password")
                });
            }
            foreach (var admin in adminUsers)
            {
                var adminUser = await userManager.FindByEmailAsync(admin.Email);
                if (adminUser == null)
                {
                    var user = new IdentityUser
                    {
                        Email = admin.Email,
                        UserName = admin.Email,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, admin.Password); // Strong password!
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Admin");
                    }
                    else
                    {
                        // Log or throw error
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new Exception("Failed to create seed user: " + errors);
                    }
                }
            }

        }
    }
}
