using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System.Text;

using trackerApi.DbContext;
using trackerApi.EndPoints;
using trackerApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!))
        };
    });

// call 'IServiceCollection.AddAuthorization' in the application startup code.'
builder.Services.AddAuthorization( /* options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("admin", "true"));
} */ );

// these next three lines must go BEFORE the context registration
// Get the base connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Get the password from secrets
var dbPassword = builder.Configuration["DbPassword"];
// Replace the placeholder with actual password
connectionString = connectionString!.Replace("${DbPassword}", dbPassword);

// Then use this connection string in your DbContext registration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
);


//
// Register the DI stuff
// 
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy",
        builder =>
        {
            builder
                .WithOrigins(
                    "http://localhost:4200",     // Angular dev server
                    "http://localhost:4000",     // Any other local development URLs
                    "https://localhost:7221",
                    "http://localhost:5257"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });

    options.AddPolicy("ProductionPolicy",
        builder =>
        {
            builder
                .WithOrigins("https://your-production-domain.com")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("DevelopmentPolicy");
}
else
{
    app.UseCors("ProductionPolicy");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map our endpoints
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapTrackerEndpoints();

// This stays right here.
app.Run();
