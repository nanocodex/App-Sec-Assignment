using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Model
{
    public class UserSession
    {
        [Key]
        public int Id { get; set; }
        
        public required string UserId { get; set; }
        
        public required string SessionId { get; set; }
        
        public required string IpAddress { get; set; }
        
        public required string UserAgent { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime LastActivityAt { get; set; }
        
        public DateTime ExpiresAt { get; set; }
        
        public bool IsActive { get; set; }
        
        // Navigation property
        public ApplicationUser? User { get; set; }
    }
}
