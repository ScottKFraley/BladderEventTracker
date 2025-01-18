namespace trackerApi.Services;

using trackerApi.Models;


public class UserService : IUserService
{
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

    public async Task<User> GetUserById(Guid id)
    {
        await Task.Delay(10);
        throw new NotImplementedException();
    }
}
