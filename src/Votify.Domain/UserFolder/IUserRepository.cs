namespace Votify.Domain.UserFolder
{
    public interface IUserRepository
    {
        Task AddAsync(User user);
        Task<bool> ExistsByNameOrEmailAsync(string name, string email);
        Task<int> CountAsync();
        Task SaveChangesAsync();
    }
}
