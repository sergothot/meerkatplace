using Common.Shared.Application.Interfaces;
using OrderService.API.Application.Abstractions;
using OrderService.API.Application.Ordering;
using OrderService.API.Infrastructure.Persistence;
using OrderService.API.Infrastructure.Repositories;
using OrderService.API.Presentation.Endpoints;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.API.Integration.Consumers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
builder.Services.AddScoped<IProcessedIntegrationMessageRepository, ProcessedIntegrationMessageRepository>();
builder.Services.AddScoped<IUnitOfWork, OrderUnitOfWork>();
builder.Services.AddScoped<ICartService, DbCartService>();
builder.Services.AddScoped<IOrderQueryService, DbOrderQueryService>();

builder.Services.AddMassTransit(cfg =>
{
    cfg.AddEntityFrameworkOutbox<OrderDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    cfg.AddConsumer<StockReservedConsumer>();
    cfg.AddConsumer<StockReservationFailedConsumer>();
    cfg.AddConsumer<PaymentSucceededConsumer>();
    cfg.AddConsumer<PaymentFailedConsumer>();

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
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await db.Database.MigrateAsync();
}
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "docs";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API v1");
    });
}

app.MapOrderEndpoints();

app.Run();
