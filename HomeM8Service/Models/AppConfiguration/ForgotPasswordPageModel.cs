using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HomeM8Service.Models.AppConfiguration
{
    public class ForgotPasswordPageModel
    {
        public string ThirdGridInformationString { get; set; }
        public string FirstGridInformationString { get; set; }
        public string PageTitle { get; set; }
        public string NewPasswordEntryPlaceholderString { get; set; }
        public string RepeatNewPasswordEntryPlaceHolderString { get; set; }
        public string SecondGridValidationString { get; set; }
        public string UsernameEntryPlaceholderString { get; set; }
        public string SendButtonString { get; set; }
        public string SecondGridValidationEntryPlaceholderString { get; set; }
    }
}