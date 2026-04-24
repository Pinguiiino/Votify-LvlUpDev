using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.UserFolder
{
    public class GeneralUser : User
    {
        public GeneralUser() { }
        public GeneralUser(string name, string email, string password) : base(name, email, password)
        {
        }
    }
}
