namespace trackerApi.EndPoints;

using Microsoft.AspNetCore.Mvc; // Need this for [FromServices]

using trackerApi.Models;
using trackerApi.Services;


public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        // Group your endpoints under a common route
        var group = app.MapGroup("/api/users");

        group.MapGet("/", GetUsers);
        group.MapGet("/{id}", GetUser);
        group.MapPost("/", CreateUser);

        // ... other user-related endpoints
    }

    private static async Task<IResult> GetUsers([FromServices] IUserService userService)
    {
        var users = await userService.GetAllUsers();

        return Results.Ok(users);
    }

    private static async Task<IResult> GetUser(Guid id, [FromServices] IUserService userService)
    {
        var user = await userService.GetUserById(id);

        return user is null ? Results.NotFound() : Results.Ok(user);
    }

    private static async Task<IResult> CreateUser(User user, [FromServices] IUserService userService)
    {
        await Task.Delay(10);

        userService.CreateUser(user);

        return Results.Created($"/api/users/{user.Id}", user);
    }
}
