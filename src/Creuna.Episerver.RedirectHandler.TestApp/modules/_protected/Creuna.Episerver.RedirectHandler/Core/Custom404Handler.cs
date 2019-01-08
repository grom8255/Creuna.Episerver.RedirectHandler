using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Creuna.Episerver.RedirectHandler.Core.Configuration;
using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;
using EPiServer;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using EPiServer.Web;

namespace Creuna.Episerver.RedirectHandler.Core
{
    [ServiceConfiguration(typeof(Custom404Handler), Lifecycle = ServiceInstanceScope.Singleton)]
    public class Custom404Handler
    {
        private readonly CustomRedirectHandler _customRedirectHandler;
        private const string NotFoundParam = "notfound";
        private readonly RedirectConfiguration _redirectConfiguration = new RedirectConfiguration();

        public Custom404Handler(CustomRedirectHandler customRedirectHandler, IEnumerable<IRedirectLogger> redirectLoggers)
        {
            _customRedirectHandler = customRedirectHandler;
            _loggers = redirectLoggers;
        }

        private static readonly List<string> IgnoredResourceExtensions = new List<string>
        {
            "jpg",
            "gif",
            "png",
            "css",
            "js",
            "ico",
            "swf"
        };


        private static readonly ILogger Logger = LogManager.GetLogger();
        private readonly IEnumerable<IRedirectLogger> _loggers;

        public virtual RedirectAttempt HandleRequest(string referer, Uri urlNotFound)
        {
            Logger.Debug("Handling urlNotFound=" + urlNotFound?.PathAndQuery);
            return _customRedirectHandler.HandleRequest(referer, urlNotFound);
        }

        public virtual void FileNotFoundHandler(object sender, EventArgs evt)
        {
            // Check if this should be enabled
            if (_redirectConfiguration.FileNotFoundHandlerMode == FileNotFoundMode.Off)
                return;

            HttpContext context = HttpContext.Current;
            if (context == null)
            {
                Logger.Debug("No HTTPContext, returning");
                return;
            }

            if (context.Response.StatusCode != 404)
                return;

            Logger.Debug("404 response detected for " + HttpContext.Current?.Request?.Url);

            var query = context.Request.ServerVariables["QUERY_STRING"];
            if ((query != null) && query.StartsWith("404;"))
            {
                return;
            }

            Uri notFoundUri = context.Request.Url;

            // Skip resource files
            if (IsResourceFile(notFoundUri))
                return;

            // If we're only doing this for remote users, we need to test for local host
            if (_redirectConfiguration.FileNotFoundHandlerMode == FileNotFoundMode.RemoteOnly)
            {
                // Determine if we're on localhost
                bool localHost = IsLocalhost();
                if (localHost)
                {
                    Logger.Debug("Determined to be localhost, returning");
                    return;
                }
                Logger.Debug("Not localhost, handling error");
            }

            // Avoid looping forever
            if (IsInfiniteLoop(context))
                return;

            var referrer = GetReferer(context.Request.UrlReferrer);
            var redirect = HandleRequest(referrer, notFoundUri);
            if (redirect.Redirected)
                HandleRedirect(context, referrer, notFoundUri, redirect);
            else
                HandlePageNotFound(context);
        }

        private string GetPath()
        {
            throw new NotImplementedException();
        }

        private void HandlePageNotFound(HttpContext context)
        {
            string url = Get404Url();


            context.Response.Clear();
            context.Response.TrySkipIisCustomErrors = true;
            context.Server.ClearError();

            if (url == null)
            {
                context.Response.StatusCode = 404; // ? 
                context.Response.End();
            }

            // do the redirect to the 404 page here
            if (HttpRuntime.UsingIntegratedPipeline)
            {
                context.Server.TransferRequest(url, true);
            }
            else
            {
                context.RewritePath(url, false);
                IHttpHandler httpHandler = new MvcHttpHandler();
                httpHandler.ProcessRequest(context);
            }
            // return the original status code to the client
            // (this won't work in integrated pipleline mode)
            context.Response.StatusCode = 404;
        }

        private void HandleRedirect(HttpContext context, string referrer, Uri notFoundUri, RedirectAttempt redirect)
        {
            foreach (var logger in _loggers)
                logger.LogRedirect(referrer, notFoundUri.ToString(), redirect.NewUrl);
            context.Response.RedirectPermanent(redirect.NewUrl);
        }

        /// <summary>
        ///     Determines whether the specified not found URI is a resource file
        /// </summary>
        /// <param name="notFoundUri">The not found URI.</param>
        /// <returns>
        ///     <c>true</c> if it is a resource file; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsResourceFile(Uri notFoundUri)
        {
            string extension = notFoundUri.AbsolutePath;
            int extPos = extension.LastIndexOf('.');
            if (extPos > 0)
            {
                extension = extension.Substring(extPos + 1);
                if (IgnoredResourceExtensions.Contains(extension))
                {
                    // Ignoring 404 rewrite of known resource extension
                    Logger.Debug("Ignoring rewrite of '{0}'. '{1}' is a known resource extension",
                        notFoundUri.ToString(),
                        extension);

                    return true;
                }
            }
            return false;
        }

        private bool IsInfiniteLoop(HttpContext ctx)
        {
            string requestUrl = ctx.Request.Url.AbsolutePath;
            string fnfPageUrl = Get404Url();

            if (fnfPageUrl == null)
                return false;

            if (fnfPageUrl.StartsWith("~"))
                fnfPageUrl = fnfPageUrl.Substring(1);
            int posQuery = fnfPageUrl.IndexOf("?", StringComparison.Ordinal);
            if (posQuery > 0)
                fnfPageUrl = fnfPageUrl.Substring(0, posQuery);

            if (string.Compare(requestUrl, fnfPageUrl, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                Logger.Information("404 Handler detected an infinite loop to 404 page for requestUrl=" + requestUrl + ". Exiting");
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Determines whether the current request is on localhost.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if current request is localhost; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsLocalhost()
        {
            var localHost = false;
            try
            {
                if (HttpContext.Current == null
                    || HttpContext.Current.Request.UserHostAddress == null)
                    return false;
                IPAddress address = IPAddress.Parse(HttpContext.Current.Request.UserHostAddress);
                Debug.WriteLine("IP Address of user: " + address, "404Handler");

                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                Debug.WriteLine("Host Entry of local computer: " + host.HostName, "404Handler");
                localHost = address.Equals(IPAddress.Loopback) || (Array.IndexOf(host.AddressList, address) >= 0);
            }
            catch (Exception ex)
            {
                Logger.Log(Level.Warning, "Unable to determine if request originates from localhost. Assuming it is not.", ex);
                return false;
            }
            return localHost;
        }

        public static string GetReferer(Uri referer)
        {
            string refererUrl = "";
            if (referer != null)
            {
                refererUrl = referer.AbsolutePath;
                if (!string.IsNullOrEmpty(refererUrl))
                {
                    // Strip away host name in front, if local redirect

                    var siteDefinition = SiteDefinition.Current;
                    if (siteDefinition != null && siteDefinition.SiteUrl != null)
                    {
                        string hostUrl = siteDefinition.SiteUrl.ToString();
                        if (refererUrl.StartsWith(hostUrl))
                            refererUrl = refererUrl.Remove(0, hostUrl.Length);
                    }
                }
            }
            return refererUrl;
        }

        /// <summary>
        ///     Creates a url to the 404 page, containing the aspxerrorpath query
        ///     variable with information about the current request url
        /// </summary>
        /// <returns></returns>
        private string Get404Url()
        {
            string baseUrl = _redirectConfiguration.FileNotFoundHandlerPage;
            if (string.IsNullOrEmpty(baseUrl))
                return null;
            string currentUrl = HttpContext.Current.Request.Url.PathAndQuery;
            
            // urlbuilder ?
            var builder = new UrlBuilder(baseUrl)
            {
                QueryCollection =
                {
                    [NotFoundParam] = currentUrl
                }
            };
            var result = builder.ToString();
            return result;

            // 2 problems: no proper separator added and currentUrl is encoded
            // return string.Concat(baseUrl, NotFoundParam, GetSeparator(baseUrl), "=", HttpContext.Current.Server.UrlEncode(currentUrl));
        }

        private string GetSeparator(string baseUrl) => baseUrl.Contains("?") ? "&" : "?";
    }
}