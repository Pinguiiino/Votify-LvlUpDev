using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.UserFolder
{
    public class Auditor : User
    {
        public Auditor() { }
        public Auditor(string name, string email, string password) : base(name, email, password)
        {
        }


    }
}
