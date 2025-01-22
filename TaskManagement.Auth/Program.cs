using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskManagement.Auth.Infrastructure.Configurations;
using TaskManagement.Auth.Infrastructure.Configurations.OpenIddict;
using TaskManagement.Auth.Infrastructure.Identity.Workers;
using TaskManagement.Auth.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("TaskManagementDbConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);

    options.UseOpenIddict();
});

builder.Services.ConfigureServices(builder.Configuration);

builder.Services.AddOpenIddictConfig(builder);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.ConfigureCors(builder.Configuration);

builder.Host.AddSerilogLogging(builder.Configuration);

builder.Services.AddRazorPages()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/Presentation/Views/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Presentation/Views/Shared/{0}.cshtml");
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var logger = services.GetRequiredService<ILogger<Program>>();

    await services.SeedRolesAsync();

    if (app.Environment.IsDevelopment())
    {
        await services.SeedDefaultAdminAsync(logger);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseStatusCodePagesWithReExecute("~/error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

// Make the implicit Program class public so test projects can access it
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-9.0#basic-tests-with-the-default-webapplicationfactory
public partial class Program { }