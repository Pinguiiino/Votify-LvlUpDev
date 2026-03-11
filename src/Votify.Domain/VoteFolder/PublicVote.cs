using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Votify.Domain.VoteFolder
{
    public class PublicVote : Vote
    {
        public PublicVote() { }

        public PublicVote(string projectId, string userId, double rawScore)
            : base(projectId, userId, rawScore) { }

        public override string VoterRole() => "PUBLIC";

        public override double NormalizedScore() => RawScore * 0.85;
    }
}
