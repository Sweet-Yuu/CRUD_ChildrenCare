using System.Net;
using System.Text.RegularExpressions;
using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRUD_ChildrenCare.Tests.Integration.Admin;

public sealed partial class ReservationsControllerTests
{
    [Fact]
    public async Task Index_RequiresStaffOrManagerRole()
    {
        await using var anonymousFactory = new TestWebApplicationFactory();
        using var anonymousClient = CreateHttpsClient(anonymousFactory);
        var anonymousResponse = await anonymousClient.GetAsync("/Admin/Reservations");
        Assert.Equal(HttpStatusCode.Redirect, anonymousResponse.StatusCode);

        await using var customerFactory = new TestWebApplicationFactory();
        await SeedUserAsync(customerFactory, SystemData.RoleValues.Customer, "cust@example.com");
        using var customerClient = CreateHttpsClient(customerFactory);
        await LoginAsync(customerClient, "cust@example.com");

        var customerResponse = await customerClient.GetAsync("/Admin/Reservations");
        Assert.Equal(HttpStatusCode.Redirect, customerResponse.StatusCode);
        Assert.Equal("/Account/AccessDenied", customerResponse.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Index_DoctorOnlySeesAssignedReservations()
    {
        await using var factory = new TestWebApplicationFactory();
        var doctorId = await SeedUserAsync(factory, SystemData.RoleValues.Doctor, "doctor1@example.com");
        var otherDoctorId = await SeedUserAsync(factory, SystemData.RoleValues.Doctor, "doctor2@example.com");

        await factory.ExecuteDbContextAsync(async context =>
        {
            var res1 = new Reservation
            {
                ReceiverFullName = "Patient Alpha",
                ReceiverEmail = "alpha@example.com",
                ReceiverMobile = "0987654321",
                ReceiverGender = Gender.Male,
                ReservedDate = DateTime.UtcNow,
                CheckupTime = DateTime.UtcNow.AddDays(1),
                Status = ReservationStatus.Submitted,
                AssignedStaffId = doctorId,
                TotalCost = 200000
            };
            var res2 = new Reservation
            {
                ReceiverFullName = "Patient Beta",
                ReceiverEmail = "beta@example.com",
                ReceiverMobile = "0987654322",
                ReceiverGender = Gender.Female,
                ReservedDate = DateTime.UtcNow,
                CheckupTime = DateTime.UtcNow.AddDays(2),
                Status = ReservationStatus.Submitted,
                AssignedStaffId = otherDoctorId,
                TotalCost = 300000
            };
            context.Reservations.AddRange(res1, res2);
            await context.SaveChangesAsync();
        });

        using var client = CreateHttpsClient(factory);
        await LoginAsync(client, "doctor1@example.com");

        var response = await client.GetAsync("/Admin/Reservations");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Patient Alpha", html);
        Assert.DoesNotContain("Patient Beta", html);
    }

    [Fact]
    public async Task Index_ManagerSeesAllReservations()
    {
        await using var factory = new TestWebApplicationFactory();
        var managerId = await SeedUserAsync(factory, SystemData.RoleValues.Manager, "mgr@example.com");
        var doctorId = await SeedUserAsync(factory, SystemData.RoleValues.Doctor, "doc@example.com");

        await factory.ExecuteDbContextAsync(async context =>
        {
            var res1 = new Reservation
            {
                ReceiverFullName = "Patient One",
                ReceiverEmail = "one@example.com",
                ReceiverMobile = "0987654321",
                ReceiverGender = Gender.Male,
                ReservedDate = DateTime.UtcNow,
                CheckupTime = DateTime.UtcNow.AddDays(1),
                Status = ReservationStatus.Submitted,
                AssignedStaffId = doctorId,
                TotalCost = 200000
            };
            var res2 = new Reservation
            {
                ReceiverFullName = "Patient Two",
                ReceiverEmail = "two@example.com",
                ReceiverMobile = "0987654322",
                ReceiverGender = Gender.Female,
                ReservedDate = DateTime.UtcNow,
                CheckupTime = DateTime.UtcNow.AddDays(2),
                Status = ReservationStatus.Submitted,
                AssignedStaffId = null,
                TotalCost = 300000
            };
            context.Reservations.AddRange(res1, res2);
            await context.SaveChangesAsync();
        });

        using var client = CreateHttpsClient(factory);
        await LoginAsync(client, "mgr@example.com");

        var response = await client.GetAsync("/Admin/Reservations");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Patient One", html);
        Assert.Contains("Patient Two", html);
    }

    [Fact]
    public async Task Cancel_RestoresAvailableQuantity()
    {
        await using var factory = new TestWebApplicationFactory();
        await SeedUserAsync(factory, SystemData.RoleValues.Manager, "mgr_cancel@example.com");

        int reservationId = 0;
        int serviceId = 0;

        await factory.ExecuteDbContextAsync(async context =>
        {
            var category = await context.Settings.FirstOrDefaultAsync(s => s.Type == SettingType.ServiceCategory);
            if (category == null)
            {
                category = new Setting { Type = SettingType.ServiceCategory, Name = "General Care", Value = "general-care", Status = SettingStatus.Active };
                context.Settings.Add(category);
                await context.SaveChangesAsync();
            }
            var service = new Service
            {
                Title = "Pediatric Checkup",
                BriefInfo = "Full checkup",
                Description = "Full pediatric checkup",
                CategoryId = category.Id,
                ListPrice = 100000,
                SalePrice = 90000,
                AvailableQuantity = 10,
                Status = ServiceStatus.Active,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            context.Services.Add(service);
            await context.SaveChangesAsync();
            serviceId = service.Id;

            var res = new Reservation
            {
                ReceiverFullName = "Cancel Patient",
                ReceiverEmail = "cancel@example.com",
                ReceiverMobile = "0987654321",
                ReceiverGender = Gender.Female,
                ReservedDate = DateTime.UtcNow,
                CheckupTime = DateTime.UtcNow.AddDays(1),
                Status = ReservationStatus.Submitted,
                TotalCost = 180000,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            context.Reservations.Add(res);
            await context.SaveChangesAsync();
            reservationId = res.Id;

            var item = new ReservationItem
            {
                ReservationId = res.Id,
                ServiceId = service.Id,
                UnitPrice = 90000,
                Quantity = 2,
                NumberOfPerson = 1,
                TotalCost = 180000
            };
            context.ReservationItems.Add(item);
            // Simulate quantity was deducted on submit (from 10 to 8)
            service.AvailableQuantity = 8;
            await context.SaveChangesAsync();
        });

        using var client = CreateHttpsClient(factory);
        await LoginAsync(client, "mgr_cancel@example.com");

        var token = await GetAntiforgeryTokenAsync(client, $"/Admin/Reservations/Details/{reservationId}");
        var cancelResponse = await client.PostAsync(
            $"/Admin/Reservations/Cancel/{reservationId}",
            Form(token, new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.Redirect, cancelResponse.StatusCode);

        await factory.ExecuteDbContextAsync(async context =>
        {
            var updatedRes = await context.Reservations.FindAsync(reservationId);
            Assert.NotNull(updatedRes);
            Assert.Equal(ReservationStatus.Cancelled, updatedRes.Status);

            var updatedService = await context.Services.FindAsync(serviceId);
            Assert.NotNull(updatedService);
            Assert.Equal(10, updatedService.AvailableQuantity); // Restored from 8 to 10
        });
    }

    private static HttpClient CreateHttpsClient(TestWebApplicationFactory factory) =>
        factory.CreateClient(new()
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

    private static async Task<int> SeedUserAsync(TestWebApplicationFactory factory, string roleName, string email)
    {
        var id = 0;
        await factory.ExecuteDbContextAsync(async context =>
        {
            var role = await context.Settings.FirstOrDefaultAsync(s => s.Type == SettingType.UserRole && s.Value == roleName);
            if (role == null)
            {
                role = new Setting
                {
                    Type = SettingType.UserRole,
                    Name = roleName,
                    Value = roleName,
                    Status = SettingStatus.Active
                };
                context.Settings.Add(role);
                await context.SaveChangesAsync();
            }

            var user = new User
            {
                FullName = $"{roleName} User",
                Gender = Gender.Other,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                Mobile = "0123456789",
                RoleId = role.Id,
                Status = UserStatus.Active
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "StrongPass1");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            id = user.Id;
        });
        return id;
    }

    private static async Task LoginAsync(HttpClient client, string email)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        var response = await client.PostAsync("/Account/Login", Form(token, new()
        {
            ["Email"] = email,
            ["Password"] = "StrongPass1",
            ["ReturnUrl"] = "/Admin/Reservations"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, $"The page {path} did not contain an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static FormUrlEncodedContent Form(string token, Dictionary<string, string> values)
    {
        values["__RequestVerificationToken"] = token;
        return new FormUrlEncodedContent(values);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();
}
