using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.Vote.VoteVoteFoler
{
    internal class ExpertVote : Vote
    {
        public int Weight { get; set; }

        public ExpertVote(string projectId, string userId, int weight) : base(projectId, userId)
        {
            this.Weight = weight;
        }
    }
}
