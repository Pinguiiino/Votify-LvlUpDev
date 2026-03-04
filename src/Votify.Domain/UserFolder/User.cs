using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.UserFolder
{
    public abstract class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        public User() { }
        public User(string name, string email, string password)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Name = name;
            this.Email = email;
            this.Password = password;
        }
    }
}
