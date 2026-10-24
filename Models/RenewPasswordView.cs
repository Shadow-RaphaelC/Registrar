using System;

namespace Models
{
    public class RenewPasswordView
    {
        public String Code { get; set; }
        public String Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}