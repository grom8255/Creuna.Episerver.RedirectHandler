using System;
using Creuna.Episerver.RedirectHandler.Core.Configuration;
using Creuna.Episerver.RedirectHandler.Core.Data;
using EPiServer.Logging;

namespace Creuna.Episerver.RedirectHandler.Core.CustomRedirects
{
    /// <summary>
    ///     Handler for custom redirects. Loads and caches the list of custom redirects
    ///     to ensure performance.
    /// </summary>
    public class CustomRedirectHandler
    {
        private readonly RedirectConfiguration _redirectConfiguration;
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(CustomRedirectHandler));
        private DictionaryCachedRedirecter _redirecter;

        public CustomRedirectHandler(RedirectConfiguration redirectConfiguration)
        {
            _redirectConfiguration = redirectConfiguration;
            _redirecter = CreateRedirecter();
        }

        private DictionaryCachedRedirecter CreateRedirecter()
        {
            return new DictionaryCachedRedirecter(new Redirecter(LoadCustomRedirects(), _redirectConfiguration));
        }

        public static string CustomRedirectHandlerException { get; set; }

        /// <summary>
        ///     Save a collection of redirects, and call method to raise an event in order to clear cache on all servers.
        /// </summary>
        /// <param name="redirects"></param>
        public void SaveCustomRedirects(CustomRedirectCollection redirects)
        {
            Logger.Log(Level.Debug, "Saving custom redirects");
            var dynamicHandler = new DataStoreHandler();
            foreach (CustomRedirect redirect in redirects)
            {
                // Add redirect 
                dynamicHandler.SaveCustomRedirect(redirect);
            }
            DataStoreEventHandlerHook.DataStoreUpdated();
        }

        /// <summary>
        ///     Read the custom redirects from the dynamic data store, and
        ///     stores them in the CustomRedirect property
        /// </summary>
        protected CustomRedirectCollection LoadCustomRedirects()
        {
            var dynamicHandler = new DataStoreHandler();
            var customRedirects = new CustomRedirectCollection();

            foreach (var redirect in dynamicHandler.GetCustomRedirects(false))
                customRedirects.Add(redirect);

            return customRedirects;
        }

        /// <summary>
        ///     Clears the redirect cache.
        /// </summary>
        public void ClearCache()
        {
            _redirecter = CreateRedirecter();
        }

        public RedirectAttempt HandleRequest(string referer, Uri urlNotFound)
        {
            return _redirecter.Redirect(referer, urlNotFound);
        }
    }
}