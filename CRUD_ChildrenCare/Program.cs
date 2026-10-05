using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Options;
using CRUD_ChildrenCare.Services.Accounts;
using CRUD_ChildrenCare.Services.Email;
using CRUD_ChildrenCare.Services.Security;
using CRUD_ChildrenCare.Services.Time;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ISecureTokenService, SecureTokenService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<IAccountService, AccountService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();
}
else
{
    builder.Services.AddOptions<SmtpOptions>()
        .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
        .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "SMTP host is required.")
        .Validate(options => options.Port is > 0 and <= 65535, "SMTP port is invalid.")
        .Validate(options => !string.IsNullOrWhiteSpace(options.FromEmail), "SMTP sender email is required.")
        .ValidateOnStart();
    builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
