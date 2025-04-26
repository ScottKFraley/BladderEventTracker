namespace trackerApi.Services;

using Microsoft.EntityFrameworkCore;

using trackerApi.DbContext;
using trackerApi.Models;


public class UserService : IUserService
{
    private AppDbContext _context;

    public UserService(AppDbContext dbContext)
    {
        _context = dbContext;
    }

    public async void CreateUser(User user)
    {
        await Task.Delay(10);
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<User>> GetAllUsers()
    {
        await Task.Delay(10);
        throw new NotImplementedException();
    }

    public async Task<User?> GetUserByUsername(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username)
            ?? throw new KeyNotFoundException($"User with username {username} not found");
    }

    public async Task<User> GetUserById(Guid id)
    {
        await Task.Delay(10);
        throw new NotImplementedException();
    }
}
