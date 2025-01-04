using Microsoft.EntityFrameworkCore;
//using Npgsql.EntityFrameworkCore.PostgreSQL;

using trackerApi.DbContext;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 
// Mapped Urls / Mappings
// 

// endpoint for GETTING tracker rows
app.MapGet(
        "/api/v1/tracker",
        async (AppDbContext context) =>
        {
            var trackedEvents = await context.TrackingLogs.ToListAsync();
            return trackedEvents;
        }
    )
    .WithName("GetTracker")
    .WithOpenApi();

app.Run();
