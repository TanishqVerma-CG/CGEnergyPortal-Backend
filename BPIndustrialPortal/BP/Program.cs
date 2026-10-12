using System.Text;
using BP.Helpers;
using BP.IService;
using BP.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

const string CorsPolicyName = "BPCorsPolicy";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors
                        .Select(e => e.ErrorMessage)
                        .Where(m => !string.IsNullOrEmpty(m))
                        .ToArray()
                );
            return new BadRequestObjectResult(new { errors });
        };
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var originsFromConfig = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

var originsFromEnv = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var allowedOrigins = originsFromConfig
    .Concat(originsFromEnv)
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
                 .AllowAnyHeader()
                .AllowAnyMethod()
              .AllowCredentials();
    });
});

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtSigningKey = jwtSettings["Key"];
if (string.IsNullOrWhiteSpace(jwtSigningKey) || Encoding.UTF8.GetByteCount(jwtSigningKey) < 32)
{
    throw new InvalidOperationException(
        "Configure Jwt:Key with a secret provider or the Jwt__Key environment variable (at least 32 bytes).");
}

var key = Encoding.UTF8.GetBytes(jwtSigningKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Billing", policy => policy.RequireRole("Billing"));
    options.AddPolicy("Broker", policy => policy.RequireRole("Broker"));
});


builder.Services.AddHangfire(config =>config.UseMemoryStorage());
builder.Services.AddHangfireServer();

// --- Application Services ---
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUsageService, UsageService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<ISupportCaseService, SupportCaseService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IAccountManagementService, AccountManagementService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBrokerCustomerService, BrokerCustomerService>();
builder.Services.AddScoped<NotificationHelper>();
builder.Services.AddScoped<BP.IService.IBrokerHomeService, BP.Services.BrokerHomeService>();
builder.Services.AddScoped<BP.IService.IBrokerRenewalsService, BP.Services.BrokerRenewalsService>();
builder.Services.AddScoped<IBrokerSupportCaseService, BrokerSupportCaseService>();
builder.Services.AddScoped<IPricingAlertService, PricingAlertService>();
builder.Services.AddScoped<IBrokerStatementsService, BrokerStatementsService>();
builder.Services.AddScoped<IBrokerDealsService, BrokerDealsService>();
builder.Services.AddScoped<IBrokerAgencyService, BrokerAgencyService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsPolicyName);

// Suppress HTTPS redirect in the integration-test environment
if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseHangfireDashboard("/hangfire");

app.Run();

// Expose Program to the test project for WebApplicationFactory<Program>
public partial class Program { }
