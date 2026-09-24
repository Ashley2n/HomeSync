using System.Security.Claims;
using api.Middleware;
using application.Interface;
using application.Services;
using infrastructure.Data;
using infrastructure.Interfaces;
using infrastructure.Middleware;
using infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using CurrentHouseholdContext = application.Services.CurrentHouseholdContext;

var builder = WebApplication.CreateBuilder(args);
var clerkIssuer = builder.Configuration["Clerk:Issuer"];
var allowedOrigins = builder.Configuration.GetSection("Clerk:AuthorizedParties").Get<string[]>()
                     ?? throw new InvalidOperationException("Clerk:AuthorizedParties must be configured.");
// e.g. ["http://localhost:3000", "https://homesync.vercel.app"]// Add services to the container.

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddHttpContextAccessor();

//DI Container

//Repository
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IHouseholdRepository, HouseholdRepository>();

// Services
builder.Services.AddScoped<ICurrentHouseholdContext, CurrentHouseholdContext>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IHouseholdService, HouseholdService>();



//CORS 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Clerk Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = clerkIssuer;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = clerkIssuer,

            // Clerk doesnt have a standard 'aud', therefore skipped
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            NameClaimType = "sub"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var azp = context.Principal?.FindFirstValue("azp");
                if (azp is null || !allowedOrigins.Contains(azp))
                    context.Fail("Token was not issued for an authorized origin.");
                return Task.CompletedTask;
            }
        };
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("My API Documentation")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowNextJs");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<HouseholdResolutionMiddleware>();
app.MapControllers();

app.Run();

public partial class Program
{
}