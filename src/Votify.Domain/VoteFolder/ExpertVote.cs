using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.VoteFoler
{
    public class ExpertVote : Vote
    {
        public int Weight { get; set; }

        public ExpertVote() { }
        public ExpertVote(string projectId, string userId, int weight) : base(projectId, userId)
        {
            this.Weight = weight;
        }
    }
}
