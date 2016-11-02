using Creuna.Episerver.RedirectHandler.Core;
using Serilog;
using System;
using Serilog.Core;

namespace Creuna.Episerver.RedirectHandler.TestApp.Logging
{
    public class SerilogRedirectLogger : IRedirectLogger
    {
        private readonly Logger _logger;

        public SerilogRedirectLogger()
        {
            _logger = new LoggerConfiguration()
                .Enrich.WithProperty("WebSite", "Creuna.Episerver.RedirectHandler.TestApp")
                .WriteTo.LiterateConsole()
                .WriteTo.RollingFile(@"c:\temp\redirectlogs\log-{Date}.log",
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] [{WebSite}] {Message}{NewLine}{Exception}"
                )
                .CreateLogger();
        }
        public void LogRedirect(string referrer, string oldUri, string newUri)
        {
            _logger.Information("Client redirected from {OldUri} to {NewUri}. Referrer:{referrer}", oldUri, newUri, referrer);
        }
    }
}