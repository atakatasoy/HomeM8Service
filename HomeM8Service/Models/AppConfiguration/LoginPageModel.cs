using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HomeM8Service.Models.AppConfiguration
{
    public class LoginPageModel
    {
        public string UsernameEntryPlaceholderString { get; set; }
        public string PasswordEntryPlaceholderString { get; set; } 
        public string LoginButtonString { get; set; }
        public string ForgotPasswordButtonString { get; set; }
        public string RegisterButtonString { get; set; }
    }
}