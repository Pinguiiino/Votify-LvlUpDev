using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.UserFolder
{
    public interface IUserRepository
    {
        Task AddAsync(User user);
        Task<bool> ExistsByNameOrEmailAsync(string name, string email);
        Task SaveChangesAsync();
    }
}
