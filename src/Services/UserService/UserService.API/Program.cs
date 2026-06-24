using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using Common.Shared.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UserService.API.Application.Abstractions;
using UserService.API.Application.Auth;
using UserService.API.Application.Users;
using UserService.API.Domain.Entities;
using UserService.API.Infrastructure.Persistence;
using UserService.API.Infrastructure.Repositories;
using UserService.API.Presentation.Endpoints;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtKey       = builder.Configuration["Jwt:Key"]!;
var jwtIssuer    = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience  = builder.Configuration["Jwt:Audience"]!;
var jwtExpiresIn = builder.Configuration.GetValue<int>("Jwt:ExpiresInSeconds", 3600);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBuyerProfileRepository, BuyerProfileRepository>();
builder.Services.AddScoped<ISellerProfileRepository, SellerProfileRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUnitOfWork, UserUnitOfWork>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "docs";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapUserServiceEndpoints(jwtKey, jwtIssuer, jwtAudience, jwtExpiresIn);

app.Run();
