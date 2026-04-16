namespace Votify.Domain.AuditFolder;

public class AuditRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public AuditRequest() { }

    public AuditRequest(string projectId)
    {
        ProjectId = projectId;
    }
}