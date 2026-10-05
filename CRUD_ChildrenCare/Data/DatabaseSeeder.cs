using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using CRUD_ChildrenCare.Services.Email;
using CRUD_ChildrenCare.Services.Security;
using CRUD_ChildrenCare.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Data;

public sealed class DatabaseSeeder(
    ApplicationDbContext context,
    IPasswordHasher<User> passwordHasher,
    ISecureTokenService tokenService,
    IEmailSender emailSender,
    ILogger<DatabaseSeeder> logger)
{
    private const string InitialAdminEmail = "admin@childrencare.local";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedSettingsAsync(cancellationToken);
        await SeedRoleMenusAsync(cancellationToken);
        await SeedAdminAsync(cancellationToken);
        await SeedTestUsersAsync(cancellationToken);
        await SeedSampleServicesAsync(cancellationToken);

        // Auto-promote shinichi2542003@gmail.com and kkk@gmail.com
        var emailsToPromote = new[] { "shinichi2542003@gmail.com", "kkk@gmail.com" };
        foreach (var email in emailsToPromote)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user != null)
            {
                var adminRoleId = await context.Settings.Where(s => s.Type == SettingType.UserRole && s.Value == SystemData.RoleValues.Admin).Select(s => s.Id).FirstOrDefaultAsync(cancellationToken);
                user.RoleId = adminRoleId;
                user.Status = UserStatus.Active; // Activate account so they don't have to verify email
            }
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSettingsAsync(CancellationToken cancellationToken)
    {
        var existingKeys = await context.Settings
            .AsNoTracking()
            .Select(item => new { item.Type, item.Name })
            .ToListAsync(cancellationToken);
        var existing = existingKeys
            .Select(item => $"{item.Type}:{item.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var roleValue in SystemData.RoleValues.All)
        {
            AddSettingIfMissing(
                existing,
                new SettingSeed(SettingType.UserRole, roleValue, roleValue, $"System role: {roleValue}"));
        }

        foreach (var seed in SystemData.AdminMenus.Concat(SystemData.SampleCategories))
        {
            AddSettingIfMissing(existing, seed);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private void AddSettingIfMissing(HashSet<string> existing, SettingSeed seed)
    {
        var key = $"{seed.Type}:{seed.Name}";
        if (!existing.Add(key))
        {
            return;
        }

        context.Settings.Add(new Setting
        {
            Type = seed.Type,
            Name = seed.Name,
            Value = seed.Value,
            Description = seed.Description,
            Status = SettingStatus.Active
        });
    }

    private async Task SeedRoleMenusAsync(CancellationToken cancellationToken)
    {
        var targetRoleValues = new[] { SystemData.RoleValues.Admin, SystemData.RoleValues.Manager, SystemData.RoleValues.Doctor, SystemData.RoleValues.Nurse };
        var roleIds = await context.Settings
            .Where(item => item.Type == SettingType.UserRole && targetRoleValues.Contains(item.Value))
            .Select(item => new { item.Value, item.Id })
            .ToListAsync(cancellationToken);

        var menuIds = await context.Settings
            .Where(item => item.Type == SettingType.AdminMenu)
            .ToListAsync(cancellationToken);

        foreach (var role in roleIds)
        {
            var existingMenuIds = await context.RoleMenus
                .Where(item => item.RoleId == role.Id)
                .Select(item => item.MenuId)
                .ToListAsync(cancellationToken);

            foreach (var menu in menuIds.Where(m => !existingMenuIds.Contains(m.Id)))
            {
                context.RoleMenus.Add(new RoleMenu { RoleId = role.Id, MenuId = menu.Id });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        var normalizedEmail = AccountValidation.NormalizeEmail(InitialAdminEmail);
        if (await context.Users.AnyAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return;
        }

        var adminRoleId = await context.Settings
            .Where(item => item.Type == SettingType.UserRole && item.Value == SystemData.RoleValues.Admin)
            .Select(item => item.Id)
            .SingleAsync(cancellationToken);
        var temporaryPassword = $"Aa1!{tokenService.CreateToken()[..12]}";
        var admin = new User
        {
            FullName = "System Administrator",
            Gender = Gender.Other,
            Email = InitialAdminEmail,
            NormalizedEmail = normalizedEmail,
            Mobile = "0000000000",
            RoleId = adminRoleId,
            Status = UserStatus.Active
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, temporaryPassword);
        context.Users.Add(admin);
        await context.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            new EmailMessage(
                admin.Email,
                "Children Care administrator account",
                $"Your temporary password is <strong>{temporaryPassword}</strong>. Change it after signing in."),
            cancellationToken);
        logger.LogInformation("Initial administrator account created for {Email}.", admin.Email);
    }

    private async Task SeedTestUsersAsync(CancellationToken cancellationToken)
    {
        var testAccounts = new[]
        {
            new { Email = "manager@childrencare.local", Name = "Service Manager", Role = SystemData.RoleValues.Manager },
            new { Email = "doctor@childrencare.local", Name = "Dr. Sarah Jenkins", Role = SystemData.RoleValues.Doctor },
            new { Email = "customer@childrencare.local", Name = "John Customer", Role = SystemData.RoleValues.Customer }
        };

        foreach (var acc in testAccounts)
        {
            var normalizedEmail = AccountValidation.NormalizeEmail(acc.Email);
            if (!await context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken))
            {
                var roleId = await context.Settings
                    .Where(s => s.Type == SettingType.UserRole && s.Value == acc.Role)
                    .Select(s => s.Id)
                    .SingleAsync(cancellationToken);

                var user = new User
                {
                    FullName = acc.Name,
                    Gender = Gender.Male,
                    Email = acc.Email,
                    NormalizedEmail = normalizedEmail,
                    Mobile = "0987654321",
                    RoleId = roleId,
                    Status = UserStatus.Active
                };
                user.PasswordHash = passwordHasher.HashPassword(user, "Password123!");
                context.Users.Add(user);
            }
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSampleServicesAsync(CancellationToken cancellationToken)
    {
        if (await context.Services.AnyAsync(cancellationToken))
        {
            return;
        }

        var categoryId = await context.Settings
            .Where(s => s.Type == SettingType.ServiceCategory)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (categoryId == 0) return;

        var service1 = new Service
        {
            Title = "General Pediatric Checkup",
            Thumbnail = "https://images.unsplash.com/photo-1622253692010-333f2da6031d?w=600",
            CategoryId = categoryId,
            BriefInfo = "Comprehensive physical examination and growth tracking for infants and children.",
            Description = "<h4>Pediatric Care Package</h4><p>Our general checkup includes vital sign monitoring, physical examination, growth milestones tracking, and personalized nutritional guidance.</p>",
            NumberOfPerson = 1,
            ListPrice = 500000,
            SalePrice = 450000,
            AvailableQuantity = 30,
            IsFeatured = true,
            Status = ServiceStatus.Active
        };

        var service2 = new Service
        {
            Title = "Pediatric Dental Examination",
            Thumbnail = "https://images.unsplash.com/photo-1588776814546-1ffcf47267a5?w=600",
            CategoryId = categoryId,
            BriefInfo = "Gentle dental checkup, cavity prevention, and teeth cleaning for children.",
            Description = "<h4>Pediatric Dentistry</h4><p>Ensuring healthy smiles from an early age. Includes oral hygiene assessment, gentle cleaning, and fluoridation.</p>",
            NumberOfPerson = 1,
            ListPrice = 350000,
            SalePrice = 300000,
            AvailableQuantity = 20,
            IsFeatured = true,
            Status = ServiceStatus.Active
        };

        var service3 = new Service
        {
            Title = "Child Vaccination & Immunization",
            Thumbnail = "https://images.unsplash.com/photo-1632053002928-1906a5829633?w=600",
            CategoryId = categoryId,
            BriefInfo = "Standard childhood vaccines administered by certified healthcare professionals.",
            Description = "<h4>Vaccination Service</h4><p>Keep your child protected against preventable diseases according to standard international immunization schedules.</p>",
            NumberOfPerson = 1,
            ListPrice = 650000,
            SalePrice = 600000,
            AvailableQuantity = 50,
            IsFeatured = false,
            Status = ServiceStatus.Active
        };

        context.Services.AddRange(service1, service2, service3);
        await context.SaveChangesAsync(cancellationToken);
    }
}
