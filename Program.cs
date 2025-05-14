// Load .env file first
using CodeVault.Data;
using CodeVault.Services;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Add environment variables from .env to configuration
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IOpenAiService, OpenAiService>();
builder.Services.AddDbContext<CodeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<CodeService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddHttpClient<VectorEmbeddingService>();
builder.Services.AddScoped<VectorEmbeddingService>();
builder.Services.AddScoped<CodeAnalysisService>();
builder.Services.AddScoped<CodeComparisonService>();
builder.Services.AddScoped<SecurityAnalysisService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<CodeDbContext>();
        dbContext.Database.Migrate();
        Console.WriteLine("Successfully applied migrations");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        Console.WriteLine($"Error applying migrations: {ex.Message}");
    }
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chat}/{action=Index}/{id?}");

app.Run();