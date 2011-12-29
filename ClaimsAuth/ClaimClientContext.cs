using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.SharePoint.Client;


namespace Kerwal.SharepointOnline
{
    public static class ClaimClientContext
    {
        /// <summary>
        /// Displays a pop up to login the user. An authentication Cookie is returned if the user is sucessfully authenticated.
        /// </summary>
        /// <param name="targetSiteUrl"></param>
        /// <param name="popUpWidth"></param>
        /// <param name="popUpHeight"></param>
        /// <returns></returns>
        public static CookieCollection GetAuthenticatedCookies(string targetSiteUrl, string username, string password)
        {
            CookieCollection authCookie = null;
            using (ClaimsWebAuth webAuth = new ClaimsWebAuth(targetSiteUrl, username, password))
            {
                authCookie = webAuth.Login();
            }
            return authCookie;
        }

        /// <summary>
        /// This method will return a ClientContext object with the authentication cookie set.
        /// The ClientContext should be disposed of as any other IDisposable
        /// </summary>
        /// <param name="targetSiteUrl"></param>
        /// <returns></returns>
        public static ClientContext GetAuthenticatedContext(string targetSiteUrl, string username, string password)
        {
            CookieCollection cookies = null;
            cookies = ClaimClientContext.GetAuthenticatedCookies(targetSiteUrl, username, password);
            if (cookies == null) return null;

            ClientContext context = new ClientContext(targetSiteUrl);
            try
            {
                context.ExecutingWebRequest += delegate(object sender, WebRequestEventArgs e)
                {
                    e.WebRequestExecutor.WebRequest.CookieContainer = new CookieContainer();
                    foreach (Cookie cookie in cookies)
                    {
                        e.WebRequestExecutor.WebRequest.CookieContainer.Add(cookie);
                    }
                };
            }
            catch
            {
                if (context != null) context.Dispose();
                throw;
            }

            return context;
        }
    }
}
