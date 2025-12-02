using Microsoft.AspNetCore.Identity;

namespace CoreTripRex.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; }
        public int LegacyUserId { get; set; } 
    }
}

