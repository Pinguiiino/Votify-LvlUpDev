using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.VoteFolder;

public class ExpertVote : Vote
{
    public ExpertVote() { }

    public ExpertVote(string projectId, string userId, double rawScore)
        : base(projectId, userId, rawScore) { }

    public override string VoterRole() => "EXPERT";

    public override double NormalizedScore() => RawScore * 1.20;
}







