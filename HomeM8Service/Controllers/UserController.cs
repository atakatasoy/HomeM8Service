using HomeM8Service.Attributes;
using HomeM8Service.Attributes.ActionFilters;
using HomeM8Service.Models;
using HomeM8Service.ServiceResponses;
using HomeM8Service.Utility;
using Newtonsoft.Json;
using SimpleCrypto;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Xml;

namespace HomeM8Service.Controllers
{
    public class UserController : ApiController
    {
        #region EstablishSharedSecret

        [HttpPost]
        public async Task<HttpResponseMessage> EstablishSharedSecret(string username, [FromBody]EstablishSharedSecretParams Parameters, bool fromRegister = false)
        {
            int responseVal = 0;
            string responseText = "OK";
            bool error = false;

            #region Method Specific Variables

            string serverPublicKey = default(string);

            #endregion

            #region Parameter Controls

            if (!error)
            {
                if (string.IsNullOrWhiteSpace(Parameters.publicKey))
                {
                    responseVal = 3;
                    responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(Parameters.publicKey));
                    error = true;
                }
                if (string.IsNullOrWhiteSpace(username))
                {
                    responseVal = 3;
                    responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(username));
                    error = true;
                }
            }

            #endregion

            #region Main Process

            if (!error)
            {
                try
                {
                    #region Handshake

                    byte[] sharedSecret = null;

                    ECDHKeyExchange serverECDH = new ECDHKeyExchange();

                    serverECDH.SetClientXml(Parameters.publicKey);

                    sharedSecret = serverECDH.GenerateSharedSecret();

                    serverPublicKey = serverECDH.GetPublicKeyXmlString(); 

                    #endregion

                    using (HomeM8Entities db = new HomeM8Entities())
                    {
                        if (fromRegister)
                        {
                            #region Delete Older Register Requests

                            DateTime val = DateTime.Now.AddMinutes(-5);

                            IEnumerable<FromRegister> deleteList = db.FromRegister.Where(each => each.CreateDate.Value < val);

                            db.FromRegister.RemoveRange(deleteList);

                            await db.SaveChangesAsync();

                            #endregion

                            if (db.Users.FirstOrDefault(each => each.Username == username) == null)
                            {
                                if (db.FromRegister.FirstOrDefault(each => each.Username == username) is FromRegister registerUser)
                                {
                                    registerUser.SharedSecret = sharedSecret;
                                    registerUser.CreateDate = DateTime.Now;
                                }
                                else
                                {
                                    if (username.Length < 4 || username.Length > 12)
                                    {
                                        responseVal = 3;
                                        responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(username));
                                        error = true;
                                    }
                                    else
                                    {
                                        db.FromRegister.Add(new FromRegister
                                        {
                                            Username = username,
                                            SharedSecret = sharedSecret,
                                            CreateDate = DateTime.Now
                                        });
                                    }
                                }

                                await db.SaveChangesAsync();
                            }
                            else
                            {
                                responseVal = 7;
                                responseText = HomeM8.GetWarningString(7);
                                error = true;
                            }
                            
                        }
                        else
                        {
                            if (db.Users.FirstOrDefault(each => each.Username == username & each.State) is Users requestUser)
                            {
                                requestUser.SharedSecret = sharedSecret;

                                await db.SaveChangesAsync();
                            }
                            else
                            {
                                responseVal = 2;
                                responseText = HomeM8.GetWarningString(2);
                                error = true;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    responseVal = -5;
                    responseText = e.Message;
                    error = true;
                }
            }

            #endregion

            return new HttpResponseMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    responseVal,
                    responseText,
                    ECDHPublicKeyBase64 = (responseVal == 0) ? Convert.ToBase64String(new UTF8Encoding().GetBytes(serverPublicKey)) : null,
                    ECDHSignedPublicKeyBase64_RSA = (responseVal == 0) ? Security.SignDataRSA(serverPublicKey) : null
                })),
            };
        }

        #endregion

        #region Login

        [SecureConnectionFilter]
        [HttpPost]
        public async Task<HttpResponseMessage> Login(string username)
        {
            int responseVal = 0;
            string responseText = "OK";
            bool error = false;
            var rawContent = await Request.Content.ReadAsStringAsync();

            #region Method Specific Variables

            Users requestedUser = null;
            List<int> ConnectedHomes = null;
            var parameters = new
            {
                Password = default(string)
            };

            #endregion

            #region Parameter Controls

            if (!HomeM8.DecryptionSucceeded(rawContent))
            {
                responseVal = 3010;
                responseText = HomeM8.GetWarningString(3010);
                error = true;
            }

            if (!error)
            {
                try
                {
                    parameters = JsonConvert.DeserializeAnonymousType(rawContent, parameters);
                }
                catch
                {
                    responseVal = 6;
                    responseText = HomeM8.GetWarningString(6);
                    error = true;
                }
            }

            if (!error)
            {
                if (string.IsNullOrWhiteSpace(parameters.Password) || string.IsNullOrWhiteSpace(username))
                {
                    responseVal = 1;
                    responseText = HomeM8.GetWarningString(1);
                    error = true;
                }
            }

            #endregion

            #region Main Process

            if (!error)
            {
                using (HomeM8Entities db = new HomeM8Entities())
                {
                    requestedUser = db.Users.FirstOrDefault(each => each.Username == username && each.State);

                    if (requestedUser != null)
                    {
                        PBKDF2 hashing = new PBKDF2();

                        var hashedPassword = hashing.Compute(parameters.Password, requestedUser.Salt);

                        if (hashedPassword != requestedUser.Userpass)
                        {
                            responseVal = 5;
                            responseText = HomeM8.GetWarningString(5);
                            error = true;
                        }

                        if (!error)
                        {
                            ConnectedHomes = db.HomeConnections.Where(each => each.UserID == requestedUser.UserID).Select(each => each.HomeID).ToList();
                        }
                    }
                    else
                    {
                        responseVal = 2;
                        responseText = HomeM8.GetWarningString(2);
                        error = true;
                    }
                }
            }

            #endregion

            var jsonStringResponse = (responseVal == 0) ?
                JsonConvert.SerializeObject(new
                {
                    responseVal,
                    responseText,
                    nameSurname = requestedUser.NameSurname,
                    accessToken = requestedUser.AccessToken,
                    ConnectedHomes,
                    requestedUser.CurrentHome,
                    userType = requestedUser.Type//Düzenle
                }) :
                JsonConvert.SerializeObject(new
                {
                    responseVal,
                    responseText
                });

            return new HttpResponseMessage()
            {
                Content = new StringContent((requestedUser != null) ? Security.EncryptAES(requestedUser.SharedSecret, jsonStringResponse) : jsonStringResponse)
            };
        }

        #endregion

        #region GetRSAPublicKey
        
        [HttpGet]
        public HttpResponseMessage GetRSAPublicKey()
        {
            string key;

            using (HomeM8Entities db = new HomeM8Entities())
            {
                key = db.RSA.Take(1).Single().publicKey;
            }

            return new HttpResponseMessage()
            {
                Content = new StringContent(key)
            };
        }

        #endregion

        #region Register

        [HttpPost]
        public async Task<HttpResponseMessage> Register(string username)
        {
            int responseVal = 0;
            string responseText = "OK";
            bool error = false;
            string cipheredParameters = Request.Content.ReadAsStringAsync().Result;

            #region Method Specific Variables

            var plainParameters = new
            {
                Email = default(string),
                Password = default(string),
                PhoneNumber = default(string),
                NameSurname = default(string)
            };

            byte[] sharedSecret = null;

            #endregion

            #region Parameters Control

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(cipheredParameters))
            {
                responseVal = 1;
                responseText = HomeM8.GetWarningString(1);
                error = true;
            }

            #endregion

            #region Main Process

            if (!error)
            {
                using (HomeM8Entities db = new HomeM8Entities())
                {
                    if (db.FromRegister.FirstOrDefault(each => each.Username == username) is FromRegister registeredUser)
                    {

                        sharedSecret = registeredUser.SharedSecret;

                        #region Decryption

                        try
                        {
                            string plainJsonString = Security.DecryptAES(registeredUser.SharedSecret, cipheredParameters);
                            try
                            {
                                plainParameters = JsonConvert.DeserializeAnonymousType(plainJsonString, plainParameters);
                            }
                            catch
                            {
                                responseVal = 6;
                                responseText = HomeM8.GetWarningString(6);
                                error = true;
                            }
                        }
                        catch (Exception)
                        {
                            responseVal = 3;
                            responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(cipheredParameters));
                            error = true;
                        }

                        #endregion

                        #region Plain Parameters Control

                        if (!error)
                        {
                            if(string.IsNullOrWhiteSpace(plainParameters.Email) && 
                                string.IsNullOrWhiteSpace(plainParameters.NameSurname) &&
                                string.IsNullOrWhiteSpace(plainParameters.Password) &&
                                string.IsNullOrWhiteSpace(plainParameters.PhoneNumber))
                            {
                                responseVal = 2008;
                                responseText = HomeM8.GetWarningString(2008);
                                error = true;
                            }
                            else
                            {
                                if (!(new Regex(@"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                                    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$", RegexOptions.IgnoreCase)
                                    .IsMatch(plainParameters.Email)))
                                {
                                    responseVal = 3;
                                    responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(plainParameters.Email));
                                    error = true;
                                }
                                else if (!HomeM8.EmailValid(plainParameters.Email))
                                {
                                    responseVal = 3008;
                                    responseText = HomeM8.GetWarningString(3008);
                                    error = true;
                                }
                                else
                                {
                                    if (plainParameters.Password.Length < 6 || plainParameters.Password.Length > 12)
                                    {
                                        responseVal = 3;
                                        responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(plainParameters.Password));
                                        error = true;
                                    }
                                    else
                                    {
                                        if (plainParameters.PhoneNumber.Length != 10 || plainParameters.PhoneNumber[0] != '5')
                                        {
                                            responseVal = 3;
                                            responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(plainParameters.PhoneNumber));
                                            error = true;
                                        }
                                    }
                                }
                            }
                        }

                        #endregion

                        if (!error)
                        {
                            PBKDF2 hashing = new PBKDF2();

                            db.Users.Add(new Users
                            {
                                Username = username,
                                Userpass = hashing.Compute(plainParameters.Password),
                                Salt = hashing.Salt,
                                NameSurname = plainParameters.NameSurname,
                                ContactInfo = plainParameters.PhoneNumber,
                                Email = plainParameters.Email,
                                SharedSecret = sharedSecret,
                                AccessToken = Guid.NewGuid().ToString("N"),
                                Type = 2,
                                CreateDate = DateTime.Now,
                                State = true
                            });

                            db.FromRegister.Remove(db.FromRegister.FirstOrDefault(each => each.Username == username));

                            await db.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        responseVal = 2;
                        responseText = HomeM8.GetWarningString(2);
                        error = true;
                    }
                }
            }

            #endregion

            return new HttpResponseMessage()
            {
                Content = new StringContent(Security.EncryptAES(sharedSecret, JsonConvert.SerializeObject(new
                {
                    responseVal,
                    responseText,
                })))
            };
        }

        #endregion

        #region GetValidationCode

        [HttpGet]
        public async Task<HttpResponseMessage> GetValidationCode(string username)
        {
            int responseVal = 0;
            string responseText = "OK";
            bool error = false;

            #region Parameter Control

            if (string.IsNullOrWhiteSpace(username))
            {
                responseVal = 3;
                responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(username)); 
                error = true;
            }

            #endregion

            #region Main Process

            if (!error)
            {
                using (HomeM8Entities db = new HomeM8Entities())
                {
                    #region Delete Older ForgotPass Requests

                    {
                        var dt = DateTime.Now.AddHours(-1);

                        IEnumerable<ForgotPass> deleteList = db.ForgotPass.Where(each => each.CreateDate < dt);

                        db.ForgotPass.RemoveRange(deleteList);

                        await db.SaveChangesAsync();
                    }
                    
                    #endregion

                    if (db.Users.FirstOrDefault(each => each.Username == username && each.State) is Users requestedUser)
                    {
                        var ValidationCode = new Random().Next(100000, 999999);

                        if (db.ForgotPass.FirstOrDefault(each => each.UserID == requestedUser.UserID) is ForgotPass fRequestedUser)
                        {
                            if (fRequestedUser.AttemptCount < 3)
                            {
                                bool smsSucceeded = true;//send sms(function)
                                if (smsSucceeded)
                                {
                                    fRequestedUser.AttemptCount++;
                                    fRequestedUser.ValidationCode = ValidationCode;
                                }
                                else
                                {
                                    //sms fail
                                }
                            }
                            else
                            {
                                var dt = DateTime.Now.AddHours(-1);

                                if (fRequestedUser.CreateDate < dt)
                                {
                                    bool smsSucceeded = true;//send sms(function)
                                    if (smsSucceeded)
                                    {
                                        fRequestedUser.ValidationCode = ValidationCode;
                                        fRequestedUser.AttemptCount = 1;
                                        fRequestedUser.CreateDate = DateTime.Now;
                                    }
                                    else
                                    {
                                        //sms fail
                                    }
                                }
                                else
                                {
                                    responseVal = 1008;
                                    responseText = HomeM8.GetWarningString(1008);
                                    error = true;
                                }
                            }
                        }
                        else
                        {
                            bool smsSucceeded = true;//send sms(function)

                            if (smsSucceeded)
                            {
                                db.ForgotPass.Add(new ForgotPass
                                {
                                    UserID = requestedUser.UserID,
                                    ValidationCode = ValidationCode,
                                    AttemptCount = 1,
                                    CreateDate = DateTime.Now
                                });
                            }
                            else
                            {
                                //sms fail
                            }
                        }

                        await db.SaveChangesAsync();
                    }
                    else
                    {
                        responseVal = 2;
                        responseText = HomeM8.GetWarningString(2);
                        error = true;
                    }
                }
            }

            #endregion

            return new HttpResponseMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    responseVal,
                    responseText
                }))
            };
        }

        #endregion

        #region ValidateVCode

        [HttpPost]
        public async Task<HttpResponseMessage> ValidateVCode(string username)
        {
            int responseVal = 0;
            string responseText = "OK";
            bool error = false;
            var cipheredParameters = await Request.Content.ReadAsStringAsync();

            #region Method Specific Variables

            var plainParameters = new
            {
                ValidationCode = default(int)
            };

            byte[] sharedSecret = null;

            #endregion

            #region Parameter Controls

            if (string.IsNullOrWhiteSpace(username))
            {
                responseVal = 3;
                responseText = HomeM8.GetWarningString(3).Replace("#Parametre", nameof(username));
                error = true;
            }

            #endregion

            #region Main Process

            if (!error)
            {
                using(HomeM8Entities db=new HomeM8Entities())
                {
                    var requestedUser = db.Users
                        .Join(db.ForgotPass, u => u.UserID, fp => fp.UserID, (u, fp) => new { u, fp })
                        .FirstOrDefault(each => each.u.Username == username && each.u.State);

                    if (requestedUser != null)
                    {
                        sharedSecret = requestedUser.u.SharedSecret;

                        #region Decryption

                        try
                        {
                            string plainJsonString = Security.DecryptAES(sharedSecret, cipheredParameters);
                            try
                            {
                                plainParameters = JsonConvert.DeserializeAnonymousType(plainJsonString, plainParameters);
                            }
                            catch
                            {
                                responseVal = 6;
                                responseText = HomeM8.GetWarningString(6);
                                error = true;
                            }
                        }
                        catch
                        {
                            responseVal = 8;
                            responseText = HomeM8.GetWarningString(8);
                            error = true;
                        }

                        #endregion

                        if (!error)
                        {
                            if (requestedUser.fp.ValidationCode != plainParameters.ValidationCode)
                            {
                                responseVal = 10;
                                responseText = HomeM8.GetWarningString(10);
                                error = true;
                            }
                        }
                    }
                    else
                    {
                        responseVal = 2;
                        responseText = HomeM8.GetWarningString(2);
                        error = true;
                    }
                }
            }

            #endregion

            var jsonStringResponse = JsonConvert.SerializeObject(new
            {
                responseVal,
                responseText
            });

            return new HttpResponseMessage()
            {
                Content = new StringContent((sharedSecret != null) ? Security.EncryptAES(sharedSecret, jsonStringResponse) : jsonStringResponse)
            };
        }

        #endregion

        #region SetNewPassword

        public async Task<HttpResponseMessage> SetNewPassword(string username)
        {
            int responseVal = 0;
            string responseText = "OK";
            bool error = false;
            var cipheredParameters = await Request.Content.ReadAsStringAsync();

            #region Method Specific Variables

            var plainParameters = new
            {
                NewPassword = default(string),
                ValidationCode = default(int)
            };
            byte[] sharedSecret = null;

            #endregion

            #region Parameter Controls

            if (string.IsNullOrWhiteSpace(username))
            {
                responseVal = 3;
                responseText = HomeM8.GetWarningString(3).Replace("#Parametre", nameof(username));
                error = true;
            }

            #endregion

            #region Main Process

            if (!error)
            {
                using (HomeM8Entities db = new HomeM8Entities())
                {
                    var requestedUser = db.Users
                         .Join(db.ForgotPass, u => u.UserID, fp => fp.UserID, (u, fp) => new { u, fp })
                         .FirstOrDefault(each => each.u.Username == username && each.u.State);

                    if (requestedUser != null)
                    {
                        sharedSecret = requestedUser.u.SharedSecret;

                        #region Decryption

                        try
                        {
                            string plainJsonString = Security.DecryptAES(sharedSecret, cipheredParameters);
                            try
                            {
                                plainParameters = JsonConvert.DeserializeAnonymousType(plainJsonString, plainParameters);
                            }
                            catch
                            {
                                responseVal = 6;
                                responseText = HomeM8.GetWarningString(6);
                                error = true;
                            }
                        }
                        catch
                        {
                            responseVal = 8;
                            responseText = HomeM8.GetWarningString(8);
                            error = true;
                        }

                        #endregion

                        #region Plain Parameters Control

                        if (!error)
                        {
                            if (string.IsNullOrWhiteSpace(plainParameters.NewPassword))
                            {
                                responseVal = 3;
                                responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(plainParameters.NewPassword));
                                error = true;
                            }
                            else
                            {
                                if (plainParameters.NewPassword.Length < 6 || plainParameters.NewPassword.Length > 12)
                                {
                                    responseVal = 3009;
                                    responseText = HomeM8.GetWarningString(3009);
                                    error = true;
                                }
                            }
                        }

                        #endregion

                        if (!error)
                        {
                            if (requestedUser.fp.ValidationCode == plainParameters.ValidationCode)
                            {
                                PBKDF2 hashing = new PBKDF2();

                                requestedUser.u.Userpass = hashing.Compute(plainParameters.NewPassword);
                                requestedUser.u.Salt = hashing.Salt;

                                db.ForgotPass.Remove(requestedUser.fp);

                                await db.SaveChangesAsync();
                            }
                            else
                            {
                                responseVal = 10;
                                responseText = HomeM8.GetWarningString(10);
                                error = true;
                            }
                        }
                    }
                    else
                    {
                        responseVal = 2;
                        responseText = HomeM8.GetWarningString(2);
                        error = true;
                    }
                }
            }

            #endregion

            var jsonStringResponse = JsonConvert.SerializeObject(new
            {
                responseVal,
                responseText
            });

            return new HttpResponseMessage()
            {
                Content = new StringContent((sharedSecret != null) ? Security.EncryptAES(sharedSecret, jsonStringResponse) : jsonStringResponse)
            };
        }

        #endregion

        #region GetAccountInfo

        [SecureConnectionFilter]
        [HttpPost]
        public async Task<HttpResponseMessage> GetAccountInfo(string username)
        {
            int responseVal = 0;
            string responseText = "OK";
            bool error = false;

            var rawContent = await Request.Content.ReadAsStringAsync();

            #region Method Specific Variables

            var parameters = new
            {
                AccessToken = default(string)
            };
            Homes currentHome = null;
            Users requesterUser = null;
            var accountInfo = new
            {
                HomeName=default(string),
                HomeAddress=default(string),
                ConnectedHomesInfo = new[]
                {
                    new
                    {
                        HomeName=default(string),
                        HomeID=default(int)
                    }
                }.ToList(),
                HomeMembers=default(List<string>),
                HomeManager=default(string),
                HomeRules=default(List<string>),
                HomePermissions=default(List<string>)
            };

            #endregion

            #region Parameter Controls

            if (!HomeM8.DecryptionSucceeded(rawContent))
            {
                responseVal = 3010;
                responseText = HomeM8.GetWarningString(3010);
                error = true;
            }

            if (!error)
            {
                try
                {
                    parameters = JsonConvert.DeserializeAnonymousType(rawContent, parameters);
                }
                catch
                {
                    responseVal = 6;
                    responseText = HomeM8.GetWarningString(6);
                    error = true;
                }
            }

            if (!error)
            {
                if (string.IsNullOrWhiteSpace(parameters.AccessToken))
                {
                    responseVal = 3;
                    responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(parameters.AccessToken));
                    error = true;
                }
            }

            #endregion

            #region Main Process

            if (!error)
            {
                using(HomeM8Entities db=new HomeM8Entities())
                {
                    requesterUser = HomeM8.GetUserByAccessToken(parameters.AccessToken, db);

                    if (requesterUser == null)
                    {
                        responseVal = 3011;
                        responseText = HomeM8.GetWarningString(3011);
                        error = true;
                    }
                    else
                    {
                        if (requesterUser.CurrentHome != null)
                        {
                            currentHome = await db.Homes.FirstOrDefaultAsync(each => each.HomeID == requesterUser.CurrentHome);

                            var homeUseRules = await db.HomeUseRules.Where(each => each.HomeID == currentHome.HomeID).ToListAsync();

                            accountInfo = new
                            {
                                currentHome.HomeName,
                                HomeAddress = currentHome.Address,
                                ConnectedHomesInfo = await db.HomeConnections
                                .Join(db.Homes, hc => hc.HomeID, h => h.HomeID, (hc, h) => new { hc, h })
                                .Where(each => each.hc.UserID == requesterUser.UserID && each.hc.State)
                                .Select(each => new
                                {
                                    each.h.HomeName,
                                    each.h.HomeID
                                }).ToListAsync(),
                                HomeMembers = await db.HomeConnections
                                .Join(db.Users, hc => hc.UserID, u => u.UserID, (hc, u) => new { hc, u })
                                .Where(each => each.hc.HomeID == currentHome.HomeID && each.hc.State && each.u.State)
                                .Select(each => each.u.NameSurname)
                                .ToListAsync() ?? new List<string>(),
                                HomeManager = (await db.Users.FirstOrDefaultAsync(each => each.UserID == currentHome.CurManagerUserID)).NameSurname,
                                HomeRules = homeUseRules.Where(each => each.RuleType == 2).Select(each => each.Detail).ToList(),
                                HomePermissions = db.HomeUseRules.Where(each => each.RuleType == 1).Select(each => each.Detail).ToList()
                            };
                        }
                    }
                }
            }

            #endregion

            var jsonStringResponse = (responseVal == 0) ?
               JsonConvert.SerializeObject(new
               {
                   responseVal,
                   responseText,
                   accountInfo
               }) : JsonConvert.SerializeObject(new
               {
                   responseVal,
                   responseText
               });

            return new HttpResponseMessage()
            {
                Content = new StringContent((requesterUser != null) ? Security.EncryptAES(requesterUser.SharedSecret, jsonStringResponse) : jsonStringResponse)
            };
        }

        #endregion

        #region ChangeCurrentHome

        [SecureConnectionFilter]
        public async Task<HttpResponseMessage> ChangeCurrentHome(string username)
        {
            int responseVal = 0;
            string responseText = "OK";
            bool error = false;

            var rawContent = await Request.Content.ReadAsStringAsync();

            #region Method Specific Variables

            var parameters = new
            {
                AccessToken = default(string),
                HomeID = default(int)
            };
            Users requesterUser = null;
            Homes requestedHome = null;

            #endregion

            #region Parameter Controls

            if (!HomeM8.DecryptionSucceeded(rawContent))
            {
                responseVal = 3010;
                responseText = HomeM8.GetWarningString(3010);
                error = true;
            }

            if (!error)
            {
                try
                {
                    parameters = JsonConvert.DeserializeAnonymousType(rawContent, parameters);
                }
                catch
                {
                    responseVal = 6;
                    responseText = HomeM8.GetWarningString(6);
                    error = true;
                }
            }

            if (!error)
            {
                if (string.IsNullOrWhiteSpace(parameters.AccessToken))
                {
                    responseVal = 3;
                    responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(parameters.AccessToken));
                    error = true;
                }
                else
                {
                    if (parameters.HomeID==0)
                    {
                        responseVal = 3;
                        responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(parameters.HomeID));
                        error = true;
                    }
                }
            }

            #endregion

            #region Main Process

            if (!error)
            {
                using(HomeM8Entities db=new HomeM8Entities())
                {
                    requesterUser = HomeM8.GetUserByAccessToken(parameters.AccessToken, db);
                    requestedHome = HomeM8.GetHomeByHomeID(parameters.HomeID, db);

                    if (requesterUser == null)
                    {
                        responseVal = 3011;
                        responseText = HomeM8.GetWarningString(3011);
                        error = true;
                    }
                    else if (requestedHome == null)
                    {
                        responseVal = 3012;
                        responseText = HomeM8.GetWarningString(3012);
                        error = true;
                    }
                    else
                    {
                        if (!HomeM8.UserAuthorized(requesterUser.UserID, requestedHome.HomeID))
                        {
                            responseVal = 3013;
                            responseText = HomeM8.GetWarningString(3013);
                            error = true;
                        }
                        else
                        {
                            if (requesterUser.CurrentHome == parameters.HomeID)
                            {
                                responseVal = 3014;
                                responseText = HomeM8.GetWarningString(3014);
                                error = true;
                            }
                        }
                    }

                    if (!error)
                    {
                        requesterUser.CurrentHome = parameters.HomeID;
                        await db.SaveChangesAsync();
                    }
                }
            }

            #endregion

            var jsonStringResponse = JsonConvert.SerializeObject(new
            {
                responseVal,
                responseText
            });

            return new HttpResponseMessage()
            {
                Content = new StringContent((requesterUser != null) ? Security.EncryptAES(requesterUser.SharedSecret, jsonStringResponse) : jsonStringResponse)
            };
        }

        #endregion

        #region CheckEmail

        [HttpGet]
        public HttpResponseMessage CheckEmail(string email)
        {
            int responseVal = 0;
            string responseText = "OK";
            bool error = false;

            Thread.Sleep(3000);

            #region Parameters Control

            if (string.IsNullOrWhiteSpace(email))
            {
                responseVal = 3;
                responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(email));
                error = true;
            }

            #endregion

            #region Main Process

            if (!error)
            {
                if (!HomeM8.EmailValid(email))
                {
                    responseVal = 3008;
                    responseText = HomeM8.GetWarningString(3008);
                    error = true;
                }
            }

            #endregion

            return new HttpResponseMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    responseVal = responseVal,
                    responseText = responseText
                }))
            };
        }

        #endregion

        #region CheckUsername

        [HttpGet]
        public HttpResponseMessage CheckUsername(string username)
        {
            int responseVal = 0;
            string responseText = "OK";
            bool error = false;

            //Thread.Sleep(3000);

            #region Parameters Check

            if (string.IsNullOrWhiteSpace(username))
            {
                responseVal = 3;
                responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(username));
                error = true;
            }

            #endregion

            #region Main Process

            if (!error)
            {
                if (!HomeM8.UsernameValid(username))
                {
                    responseVal = 7;
                    responseText = HomeM8.GetWarningString(7);
                    error = true;
                }
            }

            #endregion

            return new HttpResponseMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    responseVal,
                    responseText
                }))
            };
        }

        #endregion
    }
}