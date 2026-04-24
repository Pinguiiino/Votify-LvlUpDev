using System;
using System.Collections.Generic;
using System.Text;
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

        public async Task<bool> ExistsByNameOrEmailAsync(string name, string email)
        {
            var nameLower = name.Trim().ToLower();
            var emailLower = email.Trim().ToLower();

            return await _context.Users.AnyAsync(u =>
                u.Name.ToLower() == nameLower ||
                u.Email.ToLower() == emailLower);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
