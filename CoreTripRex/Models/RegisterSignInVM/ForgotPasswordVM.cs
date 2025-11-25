namespace CoreTripRex.Models.RegisterSignInVM
{
    public class ForgotPasswordVM
    {
        public string? Email { get; set; }

        public string? Question { get; set; }
        public string? Answer { get; set; }

        public bool ShowQuestion { get; set; }
    }
}
