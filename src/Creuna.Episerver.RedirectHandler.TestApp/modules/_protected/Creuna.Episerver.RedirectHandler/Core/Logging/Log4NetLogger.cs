using EPiServer.Logging.Compatibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Creuna.Episerver.RedirectHandler.Core.Logging
{
    sealed class Log4NetLogger : IRedirectLogger
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Log4NetLogger));
        public void LogRedirect(string referrer, string oldUri, string newUri)
        {
            Log.InfoFormat("Client redirected from '{0}' to '{1}'. Referrer={2}", oldUri, newUri, referrer);
        }
    }
}
