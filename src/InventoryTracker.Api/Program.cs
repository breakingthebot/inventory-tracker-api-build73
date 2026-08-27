// src/InventoryTracker.Api/Program.cs
// Application entry point configuring dependency injection, middleware pipeline, and database seeding.
// Connects to: src/InventoryTracker.Api/Data/*, src/InventoryTracker.Api/Services/*, src/InventoryTracker.Api/Middleware/*
// Created: 2026-08-26

using System.Text.Json.Serialization;
using InventoryTracker.Api.Data;
using InventoryTracker.Api.Middleware;
using InventoryTracker.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Controllers and JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configure Database Context
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase", true);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<InventoryDbContext>(options =>
{
    if (useInMemory || string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseInMemoryDatabase("InventoryTrackerDb");
    }
    else
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
        });
    }
});

// Register Domain Business Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IBarcodeService, BarcodeService>();
builder.Services.AddScoped<IBulkDataService, BulkDataService>();

// Configure OpenAPI / Swagger Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Inventory Tracker API",
        Version = "v1.0",
        Description = "Production-grade RESTful Inventory Tracker service built with ASP.NET Core 8.0, Entity Framework Core, SQL Server / In-Memory persistence, real-time stock adjustments, transaction auditing, and business valuation analytics.",
        Contact = new OpenApiContact
        {
            Name = "Inventory Tracker Engineering Team",
            Url = new Uri("https://github.com/breakingthebot/inventory-tracker-api-build73")
        }
    });

    // Include XML Documentation Comments if available
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure Middleware Pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment() || useInMemory)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory Tracker API v1.0");
        c.RoutePrefix = string.Empty; // Swagger UI at application root
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Automatically seed initial categories, products, and baseline inventory movements
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<InventoryDbContext>();
        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the inventory database.");
    }
}

app.Run();

// Make Program class public for WebApplicationFactory integration test support
public partial class Program { }
