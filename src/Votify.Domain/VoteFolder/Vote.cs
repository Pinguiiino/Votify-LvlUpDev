using System;
using System.Collections.Generic;
using System.Text;

namespace Votify.Domain.VoteFoler
{
    public abstract class Vote
    {
        public string Id { get; set; }
        public string VotedProjectId { get; set; }
        public string UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public Vote() { }
        protected Vote(string projectId, string userId)
        {
            this.Id = Guid.NewGuid().ToString();
            this.VotedProjectId = projectId;
            this.UserId = userId;
            this.CreatedAt = DateTime.UtcNow;
        }
    }
}
