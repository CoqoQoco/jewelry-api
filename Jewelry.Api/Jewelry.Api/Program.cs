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
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    );

// JWT Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]!)
            )
        };

        // ���� Events ����Ѻ��Ǩ�ͺ user status
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var dbContext = context.HttpContext.RequestServices
                    .GetRequiredService<JewelryContext>();

                // �֧ user id �ҡ token claims
                var userId = int.Parse(context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var username = context.Principal.FindFirst(ClaimTypes.Name)?.Value;

                // ��Ǩ�ͺ user status �ҡ database
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
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                context.HandleResponse(); // ��ͧ�ѹ default response

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
    });

// CORS
builder.Services.AddCors(c =>
{
    c.AddPolicy("AllowAnyOrigin", options =>
        options.WithOrigins(
            "http://localhost:7000",
            "https://localhost:7001",
            "http://localhost:5173"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
    );
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAnyOrigin");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/", () => "Welcome to Jewelry API!")
   .WithName("Home")
   .WithOpenApi();

//Check service/db connection
app.MapGet("/network-status", async context =>
{
    var loggerFactory = context.RequestServices.GetService<ILoggerFactory>();
    var log = loggerFactory.CreateLogger("network-status");
    await context.Response.WriteAsync("STATUS CHECK" + Environment.NewLine);

    var (canConnectJewelryContext, error) = await CheckConnections.CheckNpgsqlDatabase(app.Configuration.GetConnectionString("DefaultConnection"), log);
    await context.Response.WriteAsync($"JewelryContext {Environment.NewLine}{app.Configuration.GetConnectionString("DefaultDatabase")} {Environment.NewLine}" +
        $"{(canConnectJewelryContext ? "Success" : $"Fail >>>> {error}")}{Environment.NewLine}");
});

app.Run();