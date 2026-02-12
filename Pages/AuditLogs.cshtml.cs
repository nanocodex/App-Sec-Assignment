using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Model;

namespace WebApplication1.Pages
{
    [Authorize]
    public class AuditLogsModel : PageModel
    {
        private readonly AuthDbContext _context;

        public AuditLogsModel(AuthDbContext context)
        {
            _context = context;
        }

        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        public async Task OnGetAsync()
        {
            // Get audit logs for the current user, ordered by most recent first
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                AuditLogs = await _context.AuditLogs
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(50) // Limit to last 50 entries
                    .ToListAsync();
            }
        }
    }
}
