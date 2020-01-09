using HomeM8Service.Attributes.ActionFilters;
using HomeM8Service.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace HomeM8Service.Controllers
{
    [SecureConnectionFilter]
    public class HomeController : ApiController
    {
        #region SendRequestHome

        [HttpPost]
        public async Task<HttpResponseMessage> SendRequestHome(string username)
        {
            int responseVal = 0;
            string responseText = "OK";
            bool error = false;

            var rawContent = await Request.Content.ReadAsStringAsync();

            #region Method Specific Variable

            var parameters = new
            {
                AccessToken = default(string),
                HomeID = default(int)
            };
            Homes requestedHome = null;
            Users requesterUser = null;

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
                using (HomeM8Entities db = new HomeM8Entities())
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
                        db.ConnectionRequests.Add(new ConnectionRequests()
                        {
                            HomeID = requestedHome.HomeID,
                            UserID = requesterUser.UserID,
                            State = true,
                            CreateDate = DateTime.Now
                        });

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

        #region GetHomesByName

        [HttpPost]
        public async Task<HttpResponseMessage> GetHomesByName(string username)
        {
            int responseVal = 0;
            string responseText = "OK";
            bool error = false;

            var rawContent = await Request.Content.ReadAsStringAsync();

            Thread.Sleep(3000);

            #region Method Specific Variables

            var parameters = new
            {
                AccessToken = default(string),
                Substring = default(string)
            };
            Users requesterUser = null;
            var requestedHomes = new[]
            {
                new
                {
                    HomeName=default(string),
                    HomeManager=default(string),
                    PeopleCount=default(int),
                    HomeID=default(int),
                    AlreadyRequested=default(bool)
                }
            }.ToList();

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
                    if (string.IsNullOrWhiteSpace(parameters.Substring))
                    {
                        responseVal = 3;
                        responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(parameters.Substring));
                        error = true;
                    }
                }
            }

            #endregion

            #region Main Process

            if (!error)
            {
                using (HomeM8Entities db = new HomeM8Entities())
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
                        requestedHomes = await db.Homes
                            .Where(each => each.HomeName.Contains(parameters.Substring) && each.State)
                            .Select(each => new
                            {
                                each.HomeName,
                                HomeManager = db.Users.FirstOrDefault(user => user.UserID == each.CurManagerUserID).NameSurname,
                                PeopleCount = db.HomeConnections.Where(hc => hc.HomeID == each.HomeID && hc.State).Count(),
                                each.HomeID,
                                AlreadyRequested = db.ConnectionRequests.FirstOrDefault(req => req.HomeID == each.HomeID && req.UserID == requesterUser.UserID && req.State) != null
                            }).ToListAsync();
                    }
                }
            }

            #endregion

            var jsonStringResponse = (responseVal == 0) ?
                JsonConvert.SerializeObject(new
                {
                    responseVal,
                    responseText,
                    requestedHomes
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

        #region GetCalendarEvents

        [HttpPost]
        public async Task<HttpResponseMessage> GetCalendarEvents(string username)
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
            var requestedEvents = new[]
            {
                new
                {
                    PayerName=default(string),
                    EventExplanation=default(string),
                    PaymentAmount=default(decimal),
                    Paid=default(bool),
                    ExpectedDate=default(string)
                }
            }.ToList();

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
                if (!error && parameters.HomeID == 0)
                {
                    responseVal = 3;
                    responseText = HomeM8.GetWarningString(3).Replace("#Parametre#", nameof(parameters.HomeID));
                    error = true;
                }
            }

            #endregion

            #region Main Process

            if (!error)
            {
                using (HomeM8Entities db = new HomeM8Entities())
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
                    }

                    if (!error)
                    {
                        requestedEvents = await db.CalendarEvents
                        .GroupJoin(db.Users, ce => ce.PayerUserID, u => u.UserID, (ce, u) => new { ce, u })
                        .SelectMany(e => e.u.DefaultIfEmpty(), (ce, u) => new { ce.ce, u })
                        .Where(each => each.ce.HomeID == requestedHome.HomeID)
                        .Select(each => new
                        {
                            PayerName = each.u.NameSurname,
                            each.ce.EventExplanation,
                            each.ce.PaymentAmount,
                            each.ce.Paid,
                            ExpectedDate = each.ce.ExpectedDate.ToString()
                        }).ToListAsync();
                    }
                }
            }

            #endregion

            var jsonStringResponse = (responseVal == 0) ?
                JsonConvert.SerializeObject(new
                {
                    responseVal,
                    responseText,
                    requestedEvents
                }) :
                JsonConvert.SerializeObject(new
                {
                    responseVal,
                    responseText,
                });

            return new HttpResponseMessage()
            {
                Content = new StringContent((requesterUser != null) ? Security.EncryptAES(requesterUser.SharedSecret, jsonStringResponse) : jsonStringResponse)
            };
        }

        #endregion

        #region GetNotifications

        [HttpPost]
        public async Task<HttpResponseMessage> GetNotifications(string username)
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
            var notificationsList = new[]
            {
                new
                {
                    OwnerNameSurname=default(string),
                    NotificationMessage=default(string),
                    NotificationName=default(string),
                    NotificationCommentCount=default(string),
                    CreateDate=default(string),
                    ExpectedAnswerRange=default(int)
                }
            }.ToList();

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

                if (!error && parameters.HomeID == 0)
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
                    }

                    if (!error)
                    {
                        notificationsList = await db.Notifications
                            .Join(db.NotificationType, n => n.NotificationType, nt => nt.NotificationTypeID, (n, nt) => new { n, nt })
                            .Join(db.Users, main => main.n.OwnerUserID, u => u.UserID, (main, u) => new { main, u })
                            .Where(each => each.main.n.HomeID == parameters.HomeID)
                            .Select(each => new
                            {
                                OwnerNameSurname = each.u.NameSurname,
                                each.main.n.NotificationMessage,
                                each.main.nt.NotificationName,
                                NotificationCommentCount = "(" + db.NotificationComments.Where(each2 => each2.NotificationID == each.main.n.NotificationID).Count() + ")",
                                CreateDate = each.main.n.CreateDate.ToString() ?? null,
                                ExpectedAnswerRange = each.main.n.ExpectedAnswerRange ?? 0
                            }).ToListAsync();
                    }
                }
            }

            #endregion

            var jsonStringResponse = (responseVal == 0) ?
                JsonConvert.SerializeObject(new
                {
                    responseVal,
                    responseText,
                    notificationsList
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
    }
}
