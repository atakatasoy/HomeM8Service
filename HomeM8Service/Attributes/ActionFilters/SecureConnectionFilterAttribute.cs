using HomeM8Service.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace HomeM8Service.Attributes.ActionFilters
{ 
    public class SecureConnectionFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);

            if (actionContext.RequestContext.Url.Request.GetQueryNameValuePairs()?.FirstOrDefault(each => each.Key == "username").Value is string usernameValue)
            {
                using (HomeM8Entities db = new HomeM8Entities())
                {
                    if(HomeM8.GetUserByUsername(usernameValue, db) is Users requestedUser)
                    {
                        try
                        {
                            var decryptedContent = Security.DecryptAES(requestedUser.SharedSecret, actionContext.Request.Content.ReadAsStringAsync().Result);
                            actionContext.Request.Content = new StringContent(decryptedContent);
                        }
                        catch
                        {
                            actionContext.Request.Content = new StringContent(HomeM8.DecryptionFailedString);
                            return;
                        }
                    }
                    else
                    {
                        actionContext.Request.Content = new StringContent(HomeM8.UsernameNotFoundString);
                        return;
                    }
                }
            }
        }
    }
}