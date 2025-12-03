namespace CoreTripRex.Models.RegisterSignInVM
{
    public class ForgotUsernameVM
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Question { get; set; }
        public string? Answer { get; set; }
        public string? EmailResult { get; set; }

        public bool ShowQuestion { get; set; }
        public bool ShowResult { get; set; }
    }
}
