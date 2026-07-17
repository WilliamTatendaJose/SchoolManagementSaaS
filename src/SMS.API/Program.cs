using Scalar.AspNetCore;
using Serilog;
using SMS.API;
using SMS.Application;
using SMS.Infrastructure;
using SMS.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/sms-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

// Background dispatch of queued/scheduled messages.
builder.Services.AddHostedService<SMS.API.BackgroundServices.MessageOutboxProcessor>();

var app = builder.Build();

// Seed database
if (args.Contains("--seed") || app.Environment.IsDevelopment())
{
    try
    {
        await DatabaseSeeder.SeedAsync(app.Services);
        Log.Information("Database seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while seeding the database");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("SMS API V1");
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Add Rate Limiting middleware (should be early in pipeline)
app.UseRateLimiter();

// Add Output Caching middleware
app.UseOutputCache();

app.UseAuthentication();

// Must run after authentication (needs the JWT claims) but before authorization:
// permission checks query tenant-scoped data, and the tenant filter only matches
// once ITenantService has been populated from the token.
app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();
