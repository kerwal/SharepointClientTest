using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Net.Security;
using System.Threading;

using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Kerwal.SharepointOnline
{
    /// <summary>
    /// Provides means to authenticate a user via a pop up login form.
    /// </summary>
    public class ClaimsWebAuth : IDisposable
    {
        #region Construction

        /// <summary>
        /// Displays a pop up window to authenticate the user
        /// </summary>
        /// <param name="targetSiteUrl"></param>
        /// <param name="popUpWidth"></param>
        /// <param name="popUpHeight"></param>
        public ClaimsWebAuth(string targetSiteUrl, string username, string password)
        {
            if (string.IsNullOrEmpty(targetSiteUrl)) throw new ArgumentException(Constants.MSG_REQUIRED_SITE_URL);
            this._targetSiteUrl = targetSiteUrl;
            if (string.IsNullOrEmpty(username)) throw new ArgumentException("The username is required");
            this._username = username;
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("The password is required");
            this._password = password;

            this._webBrowser = new WebBrowser();
            this._webBrowser.Navigated += new WebBrowserNavigatedEventHandler(ClaimsWebBrowser_Navigated);
            this._webBrowser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser_DocumentCompleted);
            this._webBrowser.ScriptErrorsSuppressed = true;
        }

        void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            this._webBrowser.DocumentCompleted -= new WebBrowserDocumentCompletedEventHandler(webBrowser_DocumentCompleted);
            // sign in
            new Thread(new ThreadStart(delegate()
                {
                    try
                    {
                        HtmlElement usernameAnchor = null;
                        HtmlElement closeButton = null;
                        HtmlElement usernameField = null;
                        HtmlElement passwordField = null;
                        HtmlElement submitButton = null;

                        // 1. find the username element
                        while (!stopAutoLoginThread && usernameAnchor == null)
                        {
                            lock (this.invokeMe)
                            {
                                this.invokeMe = new Action(delegate()
                                    {
                                        usernameAnchor = this._webBrowser.Document.GetElementById("idA_Tile_Username1");
                                    });
                            }
                            Thread.Sleep(101);
                        }
                        if (stopAutoLoginThread) return;
                        // 2. if the username is not expected find and click the close button
                        if (!usernameAnchor.InnerText.Trim().Equals(this.Username))
                        {
                            while (!stopAutoLoginThread && closeButton == null)
                            {
                                lock (this.invokeMe)
                                {
                                    this.invokeMe = new Action(delegate()
                                        {
                                            closeButton = this._webBrowser.Document.GetElementById("idA_Tile_RemoveTile1");
                                            if (closeButton != null)
                                                closeButton.RaiseEvent("onclick");
                                        });
                                }
                                Thread.Sleep(101);
                            }
                            // 2a. find the username field and enter the new username
                            while (!stopAutoLoginThread && usernameField == null)
                            {
                                lock (this.invokeMe)
                                {
                                    this.invokeMe = new Action(delegate()
                                        {
                                            usernameField = this._webBrowser.Document.GetElementById("i0116");
                                            if (usernameField != null)
                                                usernameField.SetAttribute("value", this.Username);
                                        });
                                }
                                Thread.Sleep(101);
                            }
                            // 2b. find the password field and enter the password
                            while (!stopAutoLoginThread && passwordField == null)
                            {
                                lock (this.invokeMe)
                                {
                                    this.invokeMe = new Action(delegate()
                                        {
                                            passwordField = this._webBrowser.Document.GetElementById("i0118");
                                            if (usernameField != null) passwordField.SetAttribute("value", this.Password);
                                        });
                                }
                                Thread.Sleep(101);
                            }
                            // 2c. find the submit button and click it
                            while (!stopAutoLoginThread && submitButton == null)
                            {
                                lock (this.invokeMe)
                                {
                                    this.invokeMe = new Action(delegate()
                                        {
                                            submitButton = this._webBrowser.Document.GetElementById("idSIButton9");
                                            if (usernameField != null) submitButton.InvokeMember("click");
                                        });
                                }
                                Thread.Sleep(101);
                            }
                            return;
                        }
                        // 3. click the box to reveal the password field
                        HtmlElement recentUserBox = null;
                        while (!stopAutoLoginThread && recentUserBox == null)
                        {
                            lock (this.invokeMe)
                            {
                                this.invokeMe = new Action(delegate()
                                    {
                                        recentUserBox = this._webBrowser.Document.GetElementById("idDiv_Tile_Highlight1");
                                        if (recentUserBox != null)
                                            recentUserBox.InvokeMember("click");
                                    });
                            }
                            Thread.Sleep(101);
                        }
                        if (stopAutoLoginThread) return;
                        // 4. find the password field and enter the password
                        while (!stopAutoLoginThread && passwordField == null)
                        {
                            lock (this.invokeMe)
                            {
                                this.invokeMe = new Action(delegate()
                                    {
                                        passwordField = this._webBrowser.Document.GetElementById("idTxtBx_PWD_Password1Pwd");
                                        if (passwordField != null)
                                            passwordField.SetAttribute("value", this.Password);
                                    });
                            }
                            Thread.Sleep(101);
                        }
                        if (stopAutoLoginThread) return;
                        // 5. find the submit button and click it
                        while (!stopAutoLoginThread && submitButton == null)
                        {
                            lock (this.invokeMe)
                            {
                                this.invokeMe = new Action(delegate()
                                    {
                                        submitButton = this._webBrowser.Document.GetElementById("idSubmit_PWD_SignIn1Pwd");
                                        if (submitButton != null)
                                        {
                                            this._attemptingLogin = true;
                                            submitButton.InvokeMember("click");
                                        }
                                    });
                            }
                            Thread.Sleep(101);
                        }
                        if (stopAutoLoginThread) return;
                    }
                    catch (ThreadInterruptedException)
                    {
                    }

                })).Start();
        }

        #endregion

        #region private Fields
        private WebBrowser _webBrowser;
        private bool _attemptingLogin = false;
        private CookieCollection _cookies = null;

        #endregion

        #region Public Properties

        private string _loginPageUrl;
        /// <summary>
        /// Login form Url
        /// </summary>
        public string LoginPageUrl
        {
            get { return _loginPageUrl; }
            set { _loginPageUrl = value; }
        }

        private Uri _navigationEndUrl;
        /// <summary>
        /// Success Url
        /// </summary>
        public Uri NavigationEndUrl
        {
            get { return _navigationEndUrl; }
            set { _navigationEndUrl = value; }
        }

        private string _targetSiteUrl = null;
        /// <summary>
        /// Target site Url
        /// </summary>
        public string TargetSiteUrl
        {
            get { return _targetSiteUrl; }
            set { _targetSiteUrl = value; }
        }

        /// <summary>
        /// Cookies returned from CLAIM server.
        /// </summary>
        public CookieCollection AuthCookies
        {
            get { return _cookies; }
        }

        private string _username;
        /// <summary>
        /// Username for authenticating with server
        /// </summary>
        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        private string _password;
        /// <summary>
        /// Password for authenticating with server
        /// </summary>
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        private object invokeMe = "";
        private bool stopLoginLoop = false;
        bool stopAutoLoginThread = false;
        #endregion

        #region Public Methods

        /// <summary>
        /// Opens a Windows Forms Web Browser control to authenticate the user against an CLAIM site.
        /// </summary>
        /// <param name="popUpWidth"></param>
        /// <param name="popUpHeight"></param>
        public CookieCollection Login()
        {
            // set login page url and success url from target site
            this.GetClaimParams(this._targetSiteUrl);
            if (string.IsNullOrEmpty(this.LoginPageUrl)) throw new ApplicationException(Constants.MSG_NOT_CLAIM_SITE);

            // navigate to the login page url.
            this._webBrowser.Navigate(this.LoginPageUrl);

            while (!stopLoginLoop)
            {
                Application.DoEvents();
                if (invokeMe is Action)
                {
                    lock (invokeMe)
                    {
                        ((Action)invokeMe)();
                        invokeMe = "";
                    }
                }
            }

            // see ClaimsWebBrowser_Navigated event
            return this._cookies;
        }

        public void StopLogin()
        {
            stopAutoLoginThread = true;
            stopLoginLoop = true;
        }

        #endregion

        #region Private Methods

        private void GetClaimParams(string targetUrl)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(targetUrl);
            webRequest.Method = Constants.WR_METHOD_OPTIONS;
#if DEBUG
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(IgnoreCertificateErrorHandler);
#endif
            WebResponse response = null;
            try
            {
                response = (WebResponse)webRequest.GetResponse();
            }
            catch (WebException webEx)
            {
                response = webEx.Response;
            }
            this._navigationEndUrl = new Uri(response.Headers[Constants.CLAIM_HEADER_RETURN_URL]);
            this._loginPageUrl = (response.Headers[Constants.CLAIM_HEADER_AUTH_REQUIRED]);
        }

        private CookieCollection ExtractAuthCookiesFromUrl(string url)
        {
            Uri uriBase = new Uri(url);
            Uri uri = new Uri(uriBase, "/");
            // call WinInet.dll to get cookie.
            string stringCookie = CookieReader.GetCookie(uri.ToString());
            if (string.IsNullOrEmpty(stringCookie)) return null;
            stringCookie = stringCookie.Replace("; ", ",").Replace(";", ",");
            // use CookieContainer to parse the string cookie to CookieCollection
            CookieContainer cookieContainer = new CookieContainer();
            cookieContainer.SetCookies(uri, stringCookie);
            return cookieContainer.GetCookies(uri);
        }

        #endregion

        #region Private Events

        private void ClaimsWebBrowser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            // check whether the url is same as the navigationEndUrl.
            if (this._navigationEndUrl != null && this._navigationEndUrl.Equals(e.Url))
            {
                this._cookies = ExtractAuthCookiesFromUrl(this.LoginPageUrl);
                this.StopLogin();
            }
            else if (this._attemptingLogin)
            {
                // failed to login
                this.StopLogin();
            }
        }
       
        #endregion

        #region IDisposable Methods
        /// <summary> 
        /// Disposes of this instance. 
        /// </summary> 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._webBrowser != null) this._webBrowser.Dispose();
            }
        }

        #endregion

        #region Utilities
#if DEBUG
        private bool IgnoreCertificateErrorHandler
           (object sender,
           System.Security.Cryptography.X509Certificates.X509Certificate certificate,
           System.Security.Cryptography.X509Certificates.X509Chain chain,
           System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {

            return true;
        }
#endif // DEBUG
        #endregion
    }
}
