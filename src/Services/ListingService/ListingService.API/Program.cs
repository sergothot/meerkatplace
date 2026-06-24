using Common.Shared.Application.Interfaces;
using ListingService.API.Application.Abstractions;
using ListingService.API.Application.Catalog;
using ListingService.API.Integration.Consumers;
using ListingService.API.Infrastructure.Persistence;
using ListingService.API.Infrastructure.Repositories;
using MassTransit;
using ListingService.API.Presentation.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ListingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IInventoryStockRepository, InventoryStockRepository>();
builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();
builder.Services.AddScoped<IUnitOfWork, ListingUnitOfWork>();
builder.Services.AddScoped<IProductQueryService, DbProductQueryService>();
builder.Services.AddScoped<IProductCommandService, DbProductCommandService>();

builder.Services.AddMassTransit(cfg =>
{
    cfg.AddEntityFrameworkOutbox<ListingDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    cfg.AddConsumer<CheckoutRequestedConsumer>();
    cfg.AddConsumer<ReleaseStockRequestedConsumer>();

    cfg.UsingRabbitMq((context, busCfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        var username = builder.Configuration["RabbitMq:Username"] ?? "guest";
        var password = builder.Configuration["RabbitMq:Password"] ?? "guest";

        busCfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        busCfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ListingDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "docs";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Listing Service API v1");
    });
}

app.MapListingEndpoints();

app.Run();
