using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.UserFolder
{
    public abstract class Voter : User
    {
        public Voter() { }
        public Voter(string name, string email, string password) : base(name, email, password)
        {
        }
    }
}
