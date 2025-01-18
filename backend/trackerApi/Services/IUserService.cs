namespace trackerApi.Services;

using trackerApi.Models;


public interface IUserService
{
    /* Here are the things I need to implement in order to Service the 
     * UserEndpoints.
        
    var users = await userService.GetAllUsers();
    var user = await userService.GetUserById(id);
    await userService.CreateUser(user);

    */

    Task<IEnumerable<User>> GetAllUsers();

    Task<User> GetUserById(Guid id);

    void CreateUser(User user);
}
