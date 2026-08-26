using Microsoft.EntityFrameworkCore;
using StockFlow.API.Data;
using StockFlow.API.Interfaces;
using StockFlow.API.Models;

namespace StockFlow.API.Repositories;

//reaches into Docker sotrage
public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByUsernameAsync(string username) =>
        await context.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User> AddAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
}
