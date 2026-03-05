using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.VoteFolder
{
    public abstract class Vote
    {
        public string Id { get; set; }
        public string VotedProjectId { get; set; }
        public string UserId { get; set; }
        public double RawScore { get; set; }
        public DateTime CreatedAt { get; set; }



        protected Vote() { }
        protected Vote(string projectId, string userId, double rawScore)
        {
            this.Id = Guid.NewGuid().ToString();
            this.VotedProjectId = projectId;
            this.UserId = userId;
            this.RawScore = rawScore;
            this.CreatedAt = DateTime.UtcNow;
        }

        public abstract string VoterRole();

        public abstract double NormalizedScore();
    }
}
