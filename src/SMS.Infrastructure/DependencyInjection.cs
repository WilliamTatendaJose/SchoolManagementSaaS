using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SMS.Application.Interfaces;
using SMS.Infrastructure.Authorization;
using SMS.Infrastructure.Persistence;
using SMS.Infrastructure.Services;
using SMS.Infrastructure.Services.Messaging;
using SMS.Infrastructure.Services.Payments;
using SMS.Infrastructure.Services.Reports;
using System.Text;

namespace SMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Services
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IFileStorageService, S3FileStorageService>();
        services.AddScoped<IReportCardGenerator, ReportCardPdfGenerator>();
        services.AddScoped<IReceiptGenerator, ReceiptPdfGenerator>();

        // Messaging (SMS + WhatsApp channels)
        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));
        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));
        services.AddHttpClient<ISmsService, SmsService>();
        services.AddScoped<IMessageChannel, SmsChannel>();
        services.AddHttpClient<IMessageChannel, WhatsAppChannel>();

        // Payment gateway (Paynow)
        services.Configure<PaynowOptions>(configuration.GetSection(PaynowOptions.SectionName));
        services.AddHttpClient<IPaymentGatewayService, PaynowPaymentGatewayService>();

        // AWS S3
        services.AddDefaultAWSOptions(configuration.GetAWSOptions());
        services.AddAWSService<IAmazonS3>();

        // JWT Authentication
        var jwtSecretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured");
        var key = Encoding.UTF8.GetBytes(jwtSecretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // Authorization with permission-based policies
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddHttpContextAccessor();

        return services;
    }
}
