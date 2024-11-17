using Jewelry.Api.Controllers;
using Jewelry.Api.Extension;
using Jewelry.Data.Context;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "jewelry.api", Version = "v1" });
    options.CustomSchemaIds(type => SwashbuckleSchemaHelper.GetSchemaId(type));
});


//Register DB, Service 
builder.Services.AddInfrastructureServices(builder.Configuration);

//allow cors origin
builder.Services.AddCors(c =>
{
    c.AddPolicy("AllowAnyOrigin", options => options.WithOrigins(
                                        "http://localhost:7000",
                                        "https://localhost:7001",
                                        "http://localhost:5173"  // Frontend port ถ้ามี
                                         )
                          .AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});

var app = builder.Build();

app.UseExceptionMiddleware();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();

    app.UseSwagger();
    app.UseSwaggerUI();

    //TODO: Install swagger ui to docker container
    //app.UseSwaggerUI(c =>
    //{
    //    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jewelry V1");
    //});
}

app.UseCors("AllowAnyOrigin");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", async context =>
{
    await context.Response.WriteAsync("Welcome to Jewelry API!");
});

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
