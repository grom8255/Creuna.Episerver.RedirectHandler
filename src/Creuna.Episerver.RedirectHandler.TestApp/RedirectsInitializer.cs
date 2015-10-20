using Castle.Core;
using Creuna.Episerver.RedirectHandler.Core;
using Creuna.Episerver.RedirectHandler.Core.Configuration;
using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;
using EPiServer.Framework;
using EPiServer.ServiceLocation;

namespace Creuna.Episerver.RedirectHandler.TestApp
{
    [InitializableModule]
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class RedirectsInitializer : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Container.Configure(x =>
            {
                x.For<Custom404Handler>().Singleton().Use<Custom404Handler>();
                x.For<RedirectConfiguration>().Singleton().Use<RedirectConfiguration>();
                x.For<CustomRedirectHandler>().Singleton().Use<CustomRedirectHandler>();
            });
        }

        public void Initialize(EPiServer.Framework.Initialization.InitializationEngine context)
        {
            
        }

        public void Uninitialize(EPiServer.Framework.Initialization.InitializationEngine context)
        {
            
        }
    }
}