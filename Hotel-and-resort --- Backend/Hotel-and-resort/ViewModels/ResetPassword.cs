namespace Hotel_and_resort.ViewModels
{
    public class ResetPasswordModel
    {
        public string UserName { get; set; }
        public string VerificationCode { get; set; }
        public string NewPassword { get; set; }
    }

    public class ResetPasswordDto
    {
        public string UserName { get; set; }
        public string VerificationCode { get; set; }
        public string NewPassword { get; set; }
    }
}
