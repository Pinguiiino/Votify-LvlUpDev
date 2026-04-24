using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.UserFolder
{
    public class AuthService
    {
        private readonly IUserRepository _repository;

        public AuthService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<User> RegisterUserAsync(string name, string email, string password, string role)
        {
            bool exists = await _repository.ExistsByNameOrEmailAsync(name, email);
            if (exists)
            {
                throw new ArgumentException("El nombre de usuario o el correo electrónico ya están en uso.");
            }

            User newUser = role switch
            {
                "Organizer" => new Organizer(name, email, password),
                "GeneralUser" => new GeneralUser(name, email, password),
                _ => throw new ArgumentException("El rol seleccionado no es válido.")
            };

            await _repository.AddAsync(newUser);
            await _repository.SaveChangesAsync();

            return newUser;
        }
    }
}
