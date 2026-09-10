using System.Security.Claims;
using infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = clerkIssuer,

            // Clerk doesnt have a standard 'aud', therefore skipped
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            NameClaimType = "sub",
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var azp = context.Principal?.FindFirstValue("azp");
                if (azp is null || !allowedOrigins.Contains(azp))
                    context.Fail(("Token was not issued for an authorized origin."));
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
    });}

app.UseHttpsRedirection();
app.UseCors("AllowNextJs");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
