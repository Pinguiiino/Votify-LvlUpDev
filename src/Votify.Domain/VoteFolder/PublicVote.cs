using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Votify.Domain.VoteFoler
{
    public class PublicVote : Vote
    {
        public PublicVote() { }
        public PublicVote(string projectId, string userId) : base(projectId, userId)
        {
        }
    }
}
