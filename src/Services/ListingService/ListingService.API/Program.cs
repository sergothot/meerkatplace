using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Common.Shared.Application.Interfaces;
using ListingService.API.Application.Abstractions;
using ListingService.API.Application.Catalog;
using ListingService.API.Integration.Consumers;
using ListingService.API.Infrastructure.Persistence;
using ListingService.API.Infrastructure.Repositories;
using ListingService.API.Presentation.OpenApi;
using MassTransit;
using ListingService.API.Presentation.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

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

builder.Services.AddAuthorization();

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

if (app.Environment.IsDevelopment() || enableSwagger)
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "docs";
        options.SwaggerEndpoint(swaggerEndpoint, "Listing Service API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapListingEndpoints();

app.Run();
