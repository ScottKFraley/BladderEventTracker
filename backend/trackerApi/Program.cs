using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Serilog;

using System;
using System.Text;

using trackerApi.DbContext;
using trackerApi.EndPoints;
using trackerApi.Services;


// 1. Logger bootstrap so that I have the ability to log early startup/bootstrapping issues.
// The bootstrap logger (line 17) is meant for early startup logging, then
// gets replaced by the host logger configuration. -Per Claude.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Debug()
    //.WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting web application");
    var builder = WebApplication.CreateBuilder(args);

    // Added to see what configuration sources are available
    foreach (var provider in (builder.Configuration as IConfigurationRoot).Providers)
    {
        Log.Information("Configuration provider: {ProviderType}", provider.GetType().Name);
    }

    // Check if we're running in Visual Studio by checking for the debugger
    // You can also use an environment variable if you prefer
    if (System.Diagnostics.Debugger.IsAttached)
    {
        builder.Environment.EnvironmentName = "DevVS";
        Log.Information("Running in Visual Studio - setting environment to: {Environment}",
            builder.Environment.EnvironmentName);
    }

    Log.Information("Current environment: {Environment}", builder.Environment.EnvironmentName);

    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        // Check if running in container (DOTNET_RUNNING_IN_CONTAINER is automatically set by the base image.)
        var isRunningInContainer = bool.TryParse(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            out bool inContainer) && inContainer;

        if (isRunningInContainer)
        {
            // In container - listen on port 5000 on all interfaces
            serverOptions.ListenAnyIP(5000);
            Log.Information("Configuring Kestrel for container environment on port 5000");
        }
        else
        {
            // Local development - listen on localhost with multiple ports
            serverOptions.ListenLocalhost(5257); // HTTP
            serverOptions.ListenLocalhost(7221, options => // HTTPS
            {
                options.UseHttps();
            });
            Log.Information("Configuring Kestrel for local development on ports 5257/7221");
        }
    });

    builder.Host.UseSerilog((context, configuration) =>
    {
        configuration
            .WriteTo.Console()
            .WriteTo.Debug()
            .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
            .MinimumLevel.Information()
            .Enrich.FromLogContext();
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

    // TODO: Figure out what happens if I un-comment that `options`/Policy stuff.
    // call 'IServiceCollection.AddAuthorization' in the application startup code.'
    builder.Services.AddAuthorization( /* options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireClaim("admin", "true"));
        } */ );

    if (builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "DevVS")
    {
        // Yes, I'm using User Secrets!
        builder.Configuration.AddUserSecrets<Program>();
    }

    // Get the connection string for SQL Server
    var connectionString = ConnectionStringHelper.ProcessConnectionString(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        builder.Configuration);

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException(
            "Database connection string not found in configuration. " +
            "Ensure DefaultConnection is set in appsettings.json.");
    }

    // Register DbContext with SQL Server
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString)
    );

    //
    // Register the DI stuff
    // 
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<ITrackingLogService, TrackingLogService>();
    builder.Services.AddSingleton<TrackerEndpoints>();
    // ILogger here?

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

        var corsOrigins = builder.Configuration.GetSection("AllowedCorsOrigins").Get<string[]>();
        if (corsOrigins?.Length > 0)
        {
            options.AddPolicy("ProductionPolicy", builder =>
            {
                builder.WithOrigins(corsOrigins)
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            });
        }
    });

    Log.Information("Application built successfully. Calling '...builder.Build()'");
    var app = builder.Build();

    // After building the application
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            Log.Information("Attempting to connect to database...");
            await context.Database.CanConnectAsync();
            Log.Information("Database connection successful");

            // Add this:
            Log.Information("Running database migrations...");
            await context.Database.MigrateAsync();
            Log.Information("Database migrations completed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error connecting to database");
            throw;
        }
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "DevVS")
    {
        builder.Configuration.AddUserSecrets<Program>();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bladder Tracker API V1");
            // Added this line to serve Swagger UI at the root
            c.RoutePrefix = "swagger";
        });
    }

    app.UseCors(app.Environment.IsDevelopment() ? "DevelopmentPolicy" : "ProductionPolicy");

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    // Map our endpoints
    Log.Information("Mapping warm-up endpoints...");
    app.MapWarmUpEndpoints();
    Log.Information("Warm-up endpoints mapped successfully.");

    Log.Information("Mapping authentication endpoints...");
    app.MapAuthEndpoints();

    Log.Information("Mapping refresh token endpoints...");
    app.MapRefreshTokenEndpoints();

    Log.Information("Mapping user endpoints...");
    app.MapUserEndpoints();

    Log.Information("Mapping Tracker endpoints...");
    var trackerEndpoints = app.Services.GetRequiredService<TrackerEndpoints>();
    // set up the group mapping
    var group = app.MapGroup("/api/v1/tracker");
    // call the method that does the mapping so that the endpoint are functional!
    trackerEndpoints.MapTrackerEndpoints(group);

    Log.Information("Tracker endpoints mapped successfully.");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly: {ErrorMessage}\r\n{StackTrace}", ex.Message, ex.StackTrace);
    // You might want to include inner exception details
    if (ex.InnerException != null)
    {
        Log.Fatal(ex.InnerException, "Inner exception: {ErrorMessage}", ex.InnerException.Message);
    }
}
finally
{
    Log.CloseAndFlush();
}
