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
        var adminRoleId = await context.Settings
            .Where(item => item.Type == SettingType.UserRole && item.Value == SystemData.RoleValues.Admin)
            .Select(item => item.Id)
            .SingleAsync(cancellationToken);
        var menuIds = await context.Settings
            .Where(item => item.Type == SettingType.AdminMenu)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        var existingMenuIds = await context.RoleMenus
            .Where(item => item.RoleId == adminRoleId)
            .Select(item => item.MenuId)
            .ToListAsync(cancellationToken);

        foreach (var menuId in menuIds.Except(existingMenuIds))
        {
            context.RoleMenus.Add(new RoleMenu { RoleId = adminRoleId, MenuId = menuId });
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
}
