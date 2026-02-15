using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Model
{
    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; }
        
        public required string UserId { get; set; }
        
        public required string Token { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime ExpiresAt { get; set; }
        
        public bool IsUsed { get; set; }
        
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
