using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Services.Email;
using CRUD_ChildrenCare.Services.Files;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CRUD_ChildrenCare.Tests.Integration;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public RecordingEmailSender EmailSender { get; } = new();
    public RecordingAvatarStorage AvatarStorage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("Database:InitializeOnStartup", "false");
        builder.ConfigureServices(services =>
        {
            connection.Open();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);
            services.RemoveAll<IAvatarStorage>();
            services.AddSingleton<IAvatarStorage>(AvatarStorage);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        return host;
    }

    public async Task ExecuteDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            connection.Dispose();
        }
    }

    public sealed class RecordingEmailSender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    public sealed class RecordingAvatarStorage : IAvatarStorage
    {
        public const string NewAvatarPath = "/uploads/avatars/new.png";
        public bool FailSave { get; set; }
        public List<string> DeletedPaths { get; } = [];

        public Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken)
        {
            if (FailSave)
            {
                throw new AvatarStorageException("The avatar is invalid.");
            }

            return Task.FromResult(NewAvatarPath);
        }

        public Task DeleteAsync(string? webPath, CancellationToken cancellationToken)
        {
            if (webPath is not null)
            {
                DeletedPaths.Add(webPath);
            }

            return Task.CompletedTask;
        }
    }
}
