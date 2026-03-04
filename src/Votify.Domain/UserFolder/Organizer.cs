using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.UserFolder
{
    public class Organizer : User
    {
        public Organizer() { }
        public Organizer(string name, string email, string password) : base(name, email, password)
        {
        }
    }
}
