using Common.Shared.Application.Interfaces;
using PaymentService.API.Application.Abstractions;
using PaymentService.API.Integration.Consumers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.API.Application.Payments;
using PaymentService.API.Infrastructure.Persistence;
using PaymentService.API.Infrastructure.Repositories;
using PaymentService.API.Presentation.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IProcessedIntegrationMessageRepository, ProcessedIntegrationMessageRepository>();
builder.Services.AddScoped<IUnitOfWork, PaymentUnitOfWork>();
builder.Services.AddScoped<IPaymentService, DbPaymentService>();
builder.Services.AddScoped<IWalletService, DbWalletService>();

builder.Services.AddMassTransit(cfg =>
{
    cfg.AddEntityFrameworkOutbox<PaymentDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    cfg.AddConsumer<PaymentRequestedConsumer>();

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
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await db.Database.MigrateAsync();
}
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "docs";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service API v1");
    });
}

app.MapPaymentEndpoints();

app.Run();
