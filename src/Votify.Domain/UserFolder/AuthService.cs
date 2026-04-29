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
            var (nameExists, emailExists) = await _repository.CheckForDuplicatesAsync(name, email);

            if (emailExists)
            {
                throw new ArgumentException("Ese correo ya está registrado.");
            }
            if (nameExists)
            {
                throw new ArgumentException("El nombre de usuario ya está en uso.");
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

        public async Task<User> LoginAsync(string email, string password)
        {
            var user = await _repository.GetByEmailAsync(email);

            if (user == null || user.Password != password)
            {
                throw new ArgumentException("El correo electrónico o la contraseña son incorrectos.");
            }

            return user;
        }
        public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _repository.GetByIdAsync(userId);
            if (user == null)
                throw new ArgumentException("Usuario no encontrado.");

            if (user.Password != currentPassword)
                throw new ArgumentException("La contraseña actual es incorrecta.");

            user.Password = newPassword;
            await _repository.SaveChangesAsync();
        }
    }
}
