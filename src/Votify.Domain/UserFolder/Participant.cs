using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.UserFolder
{
    public class Participant : Voter
    {
        public Participant(string name, string email, string password) : base(name, email, password)
        {
        }
    }
}
