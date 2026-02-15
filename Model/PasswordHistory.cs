using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Model
{
    public class PasswordHistory
    {
        [Key]
        public int Id { get; set; }
        
        public required string UserId { get; set; }
        
        public required string PasswordHash { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
