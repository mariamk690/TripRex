namespace CoreTripRex.Models.RegisterSignInVM
{
    public class ResetPasswordVM
    {
        public string? Email { get; set; }
        public string? Token { get; set; }

        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}
