using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.UserFolder
{
    public class Jury : Voter
    {
        public Jury(string name, string email, string password) : base(name, email, password)
        {
        }
    }
}
