using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HomeM8Service.Models.AppConfiguration
{
    public class AppConfigurationModel
    {
        public DecidePageModel DecidePageContent { get; set; }
        public LoginPageModel LoginPageContent { get; set; }
        public RegisterPageModel RegisterPageContent { get; set; }
        public ForgotPasswordPageModel ForgotPasswordPageContent { get; set; }
        public HomePageModel HomePageContent { get; set; }
        public AccountPageModel AccountPageContent { get; set; }
        public AppModel ApplicationContent { get; set; }
        public AppColors AppColorConfiguration { get; set; }
    }
}