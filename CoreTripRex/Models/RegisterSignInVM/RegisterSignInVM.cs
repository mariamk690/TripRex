using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CoreTripRex.Models.RegisterSignInVM
{
    public class RegisterSignInVM
    {
        public bool ShowRegister { get; set; }

        // ----------------------
        // LOGIN FIELDS
        // ----------------------
        public string? LoginEmail { get; set; }
        public string? LoginPassword { get; set; }

        [ValidateNever]
        public string? LoginError { get; set; }

        // ----------------------
        // REGISTER FIELDS
        // ----------------------
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Password { get; set; }
        public string? RepeatPassword { get; set; }

        [ValidateNever]
        public string? RegisterError { get; set; }
    }
}
