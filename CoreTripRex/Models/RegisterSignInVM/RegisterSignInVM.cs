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
        // ----------------------
        // USER SECURITY QUESTIONS FIELDS
        // ----------------------
        public string? SecurityQuestion1 { get; set; }
        public string? SecurityAnswer1 { get; set; }

        public string? SecurityQuestion2 { get; set; }
        public string? SecurityAnswer2 { get; set; }

        public string? SecurityQuestion3 { get; set; }
        public string? SecurityAnswer3 { get; set; }

    }
}
