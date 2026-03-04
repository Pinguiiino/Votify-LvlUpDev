using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Votify.Domain
{
    internal class PublicVote : Vote
    {
        public PublicVote(string projectId, string userId) : base(projectId, userId)
        {
        }
    }
}
