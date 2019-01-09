using System.Web;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;
using Creuna.Episerver.RedirectHandler.Core.Logging;

namespace Creuna.Episerver.RedirectHandler.Core.Initialization
{
    /// <summary>
    ///     Global File Not Found Handler, for handling Asp.net exceptions
    /// </summary>
    [InitializableModule]
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class Custom404HandlerInitialization : IInitializableHttpModule, IConfigurableModule
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Custom404HandlerInitialization));
        public Injected<Custom404Handler> RedirectHandler { get; set; }

        public void Initialize(InitializationEngine context)
        {
            Log.Debug("404 handler initialized.");
        }

        public void Uninitialize(InitializationEngine context)
        {
            // Method intentionally left empty.
        }

        public void InitializeHttpEvents(HttpApplication application)
        {
            application.EndRequest += RedirectHandler.Service.FileNotFoundHandler;
        }

        public void Preload(string[] parameters)
        {
            // Method intentionally left empty.
        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            var container = context.StructureMap();
            if (!container.Model.HasImplementationsFor<IRedirectLogger>())
                container.Configure(x => x.For<IRedirectLogger>().Use<Log4NetLogger>());
        }
    }
}