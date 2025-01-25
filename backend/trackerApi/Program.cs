using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using System.Text;

using trackerApi.DbContext;
using trackerApi.EndPoints;
using trackerApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Might need to put this behind an environment var at some point as this is 
// only the/a local dev server.
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80);
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Add Swagger/OpenAPI support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bladder Tracker API",
        Version = "v1",
        Description = "API for Bladder Tracker application"
    });

    // Add JWT Authentication support to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bladder Tracker API V1");
    });

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
