using Microsoft.AspNetCore.Identity;

namespace CoreTripRex.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; }
    }
}

