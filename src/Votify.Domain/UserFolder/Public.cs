using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.UserFolder
{
    public class Public : Voter
    {
        public Public() { }
        public Public(string name, string email, string password) : base(name, email, password)
        {
        }
    }
}
