using HomeM8Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HomeM8Service
{
    public static class HomeM8
    {
        public static string UsernameNotFoundString { get; } = "UserNotFound";
        public static string DecryptionFailedString { get; } = "DecryptionFailed";
        public static Users GetUserByAccessToken(string accessToken, HomeM8Entities db)
        {
            return db.Users.FirstOrDefault(each => each.AccessToken == accessToken && each.State);
        }

        public static Users GetUserByUsername(string username ,HomeM8Entities db)
        {
            return db.Users.FirstOrDefault(each => each.Username == username && each.State);
        }

        public static string GetWarningString(int id)
        {
            string warningText = default(string);

            using (HomeM8Entities db = new HomeM8Entities())
            {
                warningText = db.Warnings.FirstOrDefault(each => each.WarningID == id).WarningContent;
            }
            return (warningText != default(string)) ? warningText : "Beklenmedik hata";
        }

        #region Control Methods

        public static bool UserAuthorized(int UserID, int homeId)
        {
            bool response = false;

            using (HomeM8Entities db = new HomeM8Entities())
            {
                if (db.HomeConnections.Where(each => each.HomeID == homeId).FirstOrDefault(each => each.UserID == UserID) is HomeConnections homeConnection)
                {
                    response = true;
                }
            }

            return response;
        }

        public static bool EmailValid(string email)
        {
            bool validEmail = false;

            using (HomeM8Entities db = new HomeM8Entities())
            {
                validEmail = db.Users.FirstOrDefault(each => each.Email == email) == null;
            }

            return validEmail;
        }

        public static bool UsernameValid(string username)
        {
            bool validUsername = false;

            using(HomeM8Entities db=new HomeM8Entities())
            {
                validUsername = db.Users.FirstOrDefault(each => each.Username == username) == null && db.FromRegister.FirstOrDefault(each => each.Username == username) == null;
            }

            return validUsername;
        }

        internal static Homes GetHomeByHomeID(int homeID, HomeM8Entities db)
        {
            return db.Homes.FirstOrDefault(each => each.HomeID == homeID && each.State);
        }

        public static bool DecryptionSucceeded(string response)
        {
            if (response == UsernameNotFoundString || response == DecryptionFailedString) return false;
            return true;
        }

        #endregion
    }
}