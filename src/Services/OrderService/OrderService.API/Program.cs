using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Common.Shared.Application.Interfaces;
using OrderService.API.Application.Abstractions;
using OrderService.API.Application.Ordering;
using OrderService.API.Infrastructure.Clients;
using OrderService.API.Infrastructure.Persistence;
using OrderService.API.Infrastructure.Repositories;
using OrderService.API.Presentation.Endpoints;
using OrderService.API.Presentation.OpenApi;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OrderService.API.Integration.Consumers;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);
var enableSwagger = builder.Configuration.GetValue<bool>("Features:EnableSwagger");
var swaggerEndpoint = builder.Configuration["Swagger:Endpoint"] ?? "/swagger/v1/swagger.json";
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<AuthorizeOnlyOperationFilter>();

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste JWT access token. Example: eyJhbGciOi..."
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc, null)] = new List<string>()
    });
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? string.Empty;
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? string.Empty;
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? string.Empty;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("buyer", policy =>
        policy.RequireAuthenticatedUser().RequireRole("Buyer"));
});

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();
builder.Services.AddScoped<IProcessedIntegrationMessageRepository, ProcessedIntegrationMessageRepository>();
builder.Services.AddScoped<IUnitOfWork, OrderUnitOfWork>();
builder.Services.AddScoped<ICartService, DbCartService>();
builder.Services.AddScoped<IOrderQueryService, DbOrderQueryService>();
builder.Services.AddHttpClient<IProductCatalogClient, HttpProductCatalogClient>(client =>
{
    var baseUrl = builder.Configuration["ListingService:BaseUrl"] ?? "http://localhost:5002";
    client.BaseAddress = new Uri(baseUrl);
});

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
if (app.Environment.IsDevelopment() || enableSwagger)
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "docs";
        options.SwaggerEndpoint(swaggerEndpoint, "Order Service API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapOrderEndpoints();

app.Run();
