using System;
using System.Collections.Generic;

namespace Votify.Domain.VoteFolder;

public class VotingSession
{
    public string Id { get; set; }
    public string EventId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime OpenAt { get; set; }
    public DateTime CloseAt { get; set; }
    public DateTime? AdjustedCloseAt { get; set; }
    public DateTime EffectiveCloseAt => AdjustedCloseAt ?? CloseAt;
    public bool IsOpen => DateTime.UtcNow >= OpenAt && DateTime.UtcNow <= EffectiveCloseAt;
    public int? ReminderMinutesBeforeClose { get; set; }
    public bool IsManuallyAdjusted { get; set; } = false;

    public virtual List<Vote> Votes { get; set; } = new();

    public VotingSession() { }

    public VotingSession(string eventId, string name, DateTime openAt, DateTime closeAt,
                         string? description = null, int? reminderMinutesBeforeClose = null)
    {
        Id = Guid.NewGuid().ToString();
        EventId = eventId;
        Name = name;
        OpenAt = openAt;
        CloseAt = closeAt;
        Description = description;
        ReminderMinutesBeforeClose = reminderMinutesBeforeClose;
    }
}
