using Votify.Domain.UserFolder;
using Microsoft.EntityFrameworkCore;

namespace Votify.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly VotifyDbContext _context;

        public UserRepository(VotifyDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task<(bool NameExists, bool EmailExists)> CheckForDuplicatesAsync(string name, string email)
        {
            var nameLower = name.Trim().ToLower();
            var emailLower = email.Trim().ToLower();

            var matchedUser = await _context.Users.FirstOrDefaultAsync(u =>
                u.Name.ToLower() == nameLower ||
                u.Email.ToLower() == emailLower);

            if (matchedUser == null) return (false, false);

            bool nameExists = matchedUser.Name.ToLower() == nameLower;
            bool emailExists = matchedUser.Email.ToLower() == emailLower;
            return (nameExists, emailExists);
        }

        public async Task<int> CountAsync()
            => await _context.Users.CountAsync();

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower());
        }
    }
}
