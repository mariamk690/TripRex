namespace CoreTripRex.Models.RegisterSignInVM
{
    public class RegisterSignInVM
    {
        public bool ShowRegister { get; set; }
        public string LoginEmail { get; set; }
        public string LoginPassword { get; set; }
        public string LoginError { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
        public string RepeatPassword { get; set; }
        public string RegisterError { get; set; }
    }
}
