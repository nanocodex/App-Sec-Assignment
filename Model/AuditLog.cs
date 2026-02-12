namespace WebApplication1.Model
{
    public class AuditLog
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public required string Action { get; set; }
        public required DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Details { get; set; }
    }
}
