using HomeM8Service.Models;
using HomeM8Service.Models.AppConfiguration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace HomeM8Service.Controllers
{
    public class AppController : ApiController
    {
        [HttpGet]
        public void ModificateLanguageContent(/*string password*/)
        {
            //if (password=="mysupersecretapikey")
            //{
            int Turkish = 1;
            int English = 2;

            AppConfigurationModel bufferModel;
            using (HomeM8Entities db = new HomeM8Entities())
            {
                var dbItem = db.Languages.FirstOrDefault(each => each.LanguageID == 1);
                bufferModel = JsonConvert.DeserializeObject<AppConfigurationModel>(dbItem.AppContent);
                var dbItem2 = db.Languages.FirstOrDefault(each => each.LanguageID == 2);
                var bufferModel2 = JsonConvert.DeserializeObject<AppConfigurationModel>(dbItem2.AppContent);

                #region Düzenleme Bölümü
                bufferModel.AccountPageContent.ExitButtonString = "Evden Ayrıl";
                #endregion

                dbItem.AppContent = JsonConvert.SerializeObject(bufferModel);
                dbItem2.AppContent = JsonConvert.SerializeObject(bufferModel2);
                db.SaveChanges();
            }
            //}
        }

        #region GetAppConfiguration

        /// <summary>
        /// Application Content Language Provider
        /// </summary>
        /// <param name="id">1 for Turkish , 2 for English</param>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage GetAppConfiguration(int id)
        {
            #region App Language Content Template

            //AppLanguageModel bufferModel = new AppLanguageModel()
            //{
            //    DecidePageContent = new DecidePageModel()
            //    {
            //        HeaderString = "LAUNCH APPLICATION",
            //        LoginButtonString = "LOGIN",
            //        RegisterButtonString = "REGISTER"
            //    },
            //    ForgotPasswordPageContent = new ForgotPasswordPageModel()
            //    {
            //        ThirdGridInformationString = "Please enter your new password.",
            //        FirstGridInformationString = "We will send a validation code to your phone which is registered in our application.",
            //        UsernameEntryPlaceholderString = "Username",
            //        NewPasswordEntryPlaceholderString = "Password",
            //        RepeatNewPasswordEntryPlaceHolderString = "Password",
            //        SecondGridValidationString = "Please enter six digit validation code",
            //        SecondGridValidationEntryPlaceholderString = "Validation Code",
            //        SendButtonString = "Send"
            //    },
            //    LoginPageContent = new LoginPageModel()
            //    {
            //        UsernameEntryPlaceholderString = "Username",
            //        ForgotPasswordButtonString = "Forgot Password",
            //        LoginButtonString = "Login",
            //        PasswordEntryPlaceholderString = "Password"
            //    },
            //    RegisterPageContent = new RegisterPageModel()
            //    {
            //        UsernameEntryPlaceholderString = "Username",
            //        PasswordEntryPlaceholderString = "Password",
            //        RepeatPasswordEntryPlaceholderString = "Password",
            //        EmailEntryPlaceholderString = "E-Mail",
            //        NameSurnameEntryPlaceholderString = "Name Surname",
            //        PhoneEntryPlaceholderString = "Cell Phone"
            //    }
            //};

            //using (HomeM8Entities db = new HomeM8Entities())
            //{
            //    db.Languages.Add(new Languages { AppContent = JsonConvert.SerializeObject(bufferModel) });
            //    db.SaveChanges();

            //var buffer = JsonConvert.DeserializeObject<AppConfigurationModel>(db.Languages.FirstOrDefault(each => each.LanguageID == 1).AppContent);
            //buffer.AppColorConfiguration.AppInfoStringsColor = "#ffffff";
            //buffer.AppColorConfiguration.ButtonColor = "#677DAC";
            //buffer.AppColorConfiguration.ButtonTextColor = "#ffffff";
            //buffer.AppColorConfiguration.InputFrameBorderColor = "#FFFFFF";
            //buffer.AppColorConfiguration.LoginEntryBackground = "#839AC4";
            //buffer.AppColorConfiguration.PageWrapperColor = "#8DA4CE";
            //buffer.AppColorConfiguration.NavigationPrimary = "#0000ff";

            //var user = db.Languages.FirstOrDefault(each => each.LanguageID == 1);

            //var buffer2 = JsonConvert.DeserializeObject<AppConfigurationModel>(db.Languages.FirstOrDefault(each => each.LanguageID == 2).AppContent);
            //buffer2.AppColorConfiguration.AppInfoStringsColor = "#ffffff";
            //buffer2.AppColorConfiguration.ButtonColor = "#677DAC";
            //buffer2.AppColorConfiguration.ButtonTextColor = "#ffffff";
            //buffer2.AppColorConfiguration.InputFrameBorderColor = "#FFFFFF";
            //buffer2.AppColorConfiguration.LoginEntryBackground = "#839AC4";
            //buffer2.AppColorConfiguration.PageWrapperColor = "#8DA4CE";
            //buffer2.AppColorConfiguration.NavigationPrimary = "#0000ff";

            //var user2 = db.Languages.FirstOrDefault(each => each.LanguageID == 2);

            //user.AppContent = JsonConvert.SerializeObject(buffer);

            //user2.AppContent = JsonConvert.SerializeObject(buffer2);

            //db.SaveChanges();
            //}

            #endregion

            string languageJsonString = default(string);
            using (HomeM8Entities db = new HomeM8Entities())
            {
                languageJsonString = db.Languages.FirstOrDefault(each => each.LanguageID == id)?.AppContent;
            }
            return new HttpResponseMessage()
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(new
                    {
                        responseVal = 0,
                        responseText = "OK",
                        languageJsonString = languageJsonString ?? ""
                    }))
            };
        } 

        #endregion

    }
}
