using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using trackerApi.DbContext;
using trackerApi.Models;
using trackerApi.Services;

namespace trackerApi.EndPoints;

public static class AuthenticationEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", async (
            AppDbContext context,
            IConfiguration config,
            ITokenService tokenService,
            LoginDto loginDto) =>
            {
                var user = await context.Users
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    return Results.Unauthorized();
                }
                //var token = GenerateJwtToken(user!, config);
                var token = tokenService.GenerateToken(user: user);

                return Results.Ok(new { Token = token });
            })
        .WithName("Login")
        .WithOpenApi();

        // see GenerateToken() method below
        app.MapPost("/api/auth/token", GenerateToken)
            .WithName("GenerateToken")
            .WithOpenApi();

        //app.MapPost("/api/auth/register", async (
        //            AppDbContext context,
        //            RegisterDto registerDto) =>
        //{
        //    if (awaitcontext.Users.AnyAsync(u => u.Username == registerDto.Username))
        //    {
        //        returnResults.BadRequest("Username already exists");
        //    }
        //    varuser = newUser
        //            {
        //        Username = registerDto.Username,
        //        PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password)
        //    };
        //    context.Users.Add(user);
        //    awaitcontext.SaveChangesAsync();
        //    returnResults.Created($"/api/users/{user.Id}", user);
        //})
        //        .WithName("Register")
        //        .WithOpenApi();

    } // end public static void MapAuthEndpoints()

    /// <summary>
    /// Generates a new JWT.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="tokenService"></param>
    /// <remarks>
    /// This is a Minimal API 'handler' method.
    /// </remarks>
    /// <returns>
    /// An IResult instance containg a StatusCodes value.
    /// </returns>
    public static IResult GenerateToken(
        HttpContext context,
        ITokenService tokenService)
    {
        var username = context.User.Identity?.Name;

        if (string.IsNullOrEmpty(username))
        {
            return Results.Unauthorized();
        }

        var token = tokenService.GenerateToken(username: username);

        return Results.Ok(new { token });
    }

    //private static string GenerateJwtToken(User user, IConfiguration config)
    //{
    //    var jwtSettings = config.GetSection("JwtSettings");
    //    var key = new SymmetricSecurityKey(
    //        Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
    //    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    //    var claims = new[] {
    //        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    //        new Claim(ClaimTypes.Name, user.Username)
    //    };

    //    var token = new JwtSecurityToken(
    //                issuer: jwtSettings["Issuer"],
    //                audience: jwtSettings["Audience"],
    //                claims: claims,
    //                expires: DateTime.Now.AddMinutes(
    //                    double.Parse(jwtSettings["ExpirationInMinutes"] ?? "60")),
    //                signingCredentials: credentials
    //            );

    //    return new JwtSecurityTokenHandler().WriteToken(token);
    //}
}
