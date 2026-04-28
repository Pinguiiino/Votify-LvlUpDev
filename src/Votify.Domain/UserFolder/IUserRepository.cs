namespace Votify.Domain.UserFolder
{
    public interface IUserRepository
    {
        Task AddAsync(User user);
        Task<(bool NameExists, bool EmailExists)> CheckForDuplicatesAsync(string name, string email);
        Task SaveChangesAsync();
        Task<User?> GetByEmailAsync(string email);
        Task<int> CountAsync();
    }
}
