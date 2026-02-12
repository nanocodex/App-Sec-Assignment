using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Model
{
    public class ApplicationUser : IdentityUser
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string CreditCard { get; set; }  // This is encrypted
        public required string Mobile { get; set; }
        public required string Billing { get; set; }
        public required string Shipping { get; set; }
        public required string PhotoPath { get; set; }   // Store the path to the photo instead of the photo itself
    }
}
