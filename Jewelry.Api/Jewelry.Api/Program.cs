using Base.API.Extensions;
using Jewelry.Api.Extension;
using Jewelry.Data.Context;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.MaxDepth = 32; // จำกัดความลึกของ object graph
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        // ใช้ CamelCasePropertyNamesContractResolver เพื่อให้ได้ camelCase (tokenValue, userId, etc.)
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
        // เพิ่ม error handling สำหรับ serialization
        options.SerializerSettings.Error = (sender, args) =>
        {
            Console.WriteLine($"JSON Serialization Error: {args.ErrorContext.Error.Message}");
            Console.WriteLine($"Path: {args.ErrorContext.Path}");
            args.ErrorContext.Handled = true; // ข้ามไปต่อแทนที่จะ throw
        };
    });

// JWT Configuration - with null check
var jwtKey = builder.Configuration["JwtSettings:Key"] ?? "";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "jewelry-api";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "jewelry-client";

// Always add authentication, but configure based on whether JWT key exists
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Use a default key if not configured (for startup only - will fail auth but not crash)
        var keyToUse = string.IsNullOrEmpty(jwtKey) 
            ? "DefaultKeyForStartupOnlyNotForProduction32Chars!" 
            : jwtKey;
            
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(jwtKey),
            ValidateAudience = !string.IsNullOrEmpty(jwtKey),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = !string.IsNullOrEmpty(jwtKey),
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(keyToUse)
            )
        };

        // Only add events if JWT is properly configured
        if (!string.IsNullOrEmpty(jwtKey))
        {
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var dbContext = context.HttpContext.RequestServices
                        .GetRequiredService<JewelryContext>();

                    var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var username = context.Principal?.FindFirst(ClaimTypes.Name)?.Value;

                    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                    {
                        context.Fail("Invalid user claims");
                        return;
                    }

                    var user = await dbContext.TbtUser
                        .FirstOrDefaultAsync(u => u.Id == userId && u.Username == username);

                    if (user == null || !user.IsActive || user.IsNew)
                    {
                        context.Fail("User is inactive or not found");
                        return;
                    }
                },

                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },

                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var response = JsonConvert.SerializeObject(new
                    {
                        status = 401,
                        message = "You are not authorized",
                        error = context.ErrorDescription
                    });

                    return context.Response.WriteAsync(response);
                }
            };
        }
    });

// CORS - Update for production
builder.Services.AddCors(c =>
{
    c.AddPolicy("AllowAnyOrigin", options =>
    {
        var isProduction = builder.Environment.IsProduction();
        if (isProduction)
        {
            // In production, allow all origins (or configure specific ones)
            options.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        }
        else
        {
            options.WithOrigins(
                "http://localhost:7000",
                "https://localhost:7001",
                "http://localhost:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
        }
    });
});

// Learn more about configuring Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "jewelry.api", Version = "v1" });
    options.CustomSchemaIds(type => SwashbuckleSchemaHelper.GetSchemaId(type));

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

//Register DB, Service 
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<AuthenticationMiddleware>();
app.UseExceptionMiddleware();

// Enable Swagger for all environments
app.UseSwagger();
app.UseSwaggerUI();

// Remove HTTPS redirection in production (container environment)
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAnyOrigin");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint for Azure App Service
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .AllowAnonymous();

// Home endpoint
app.MapGet("/", () => Results.Json(new
{
    message = "Welcome to Jewelry API!",
    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
    jwtConfigured = !string.IsNullOrEmpty(jwtKey),
    timestamp = DateTime.UtcNow
}))
.WithName("Home")
.WithOpenApi();

// Config check endpoint
app.MapGet("/config-check", () =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return Results.Json(new
    {
        jwtKeyConfigured = !string.IsNullOrEmpty(jwtKey),
        jwtIssuer = jwtIssuer,
        jwtAudience = jwtAudience,
        connectionStringConfigured = !string.IsNullOrEmpty(connectionString),
        connectionStringPreview = !string.IsNullOrEmpty(connectionString) 
            ? connectionString.Substring(0, Math.Min(50, connectionString.Length)) + "..." 
            : "NOT SET",
        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
    });
})
.WithName("ConfigCheck")
.WithOpenApi();

// Network status endpoint
app.MapGet("/network-status", async context =>
{
    var loggerFactory = context.RequestServices.GetService<ILoggerFactory>();
    var log = loggerFactory?.CreateLogger("network-status");
    await context.Response.WriteAsync("STATUS CHECK" + Environment.NewLine);

    var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        await context.Response.WriteAsync("ERROR: DefaultConnection is not configured!" + Environment.NewLine);
        return;
    }

    var (canConnectJewelryContext, error) = await CheckConnections.CheckNpgsqlDatabase(connectionString, log);
    await context.Response.WriteAsync($"JewelryContext {Environment.NewLine}" +
        $"{(canConnectJewelryContext ? "Success" : $"Fail >>>> {error}")}{Environment.NewLine}");
});

app.Run();