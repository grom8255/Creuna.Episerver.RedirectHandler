using System;
using System.Configuration;
using Creuna.Episerver.RedirectHandler.Core;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace $rootnamespace$
{
    [InitializableModule]
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class PageMovedHandlerInitializationModule : IConfigurableModule
    {
        protected virtual IContentEvents ContentEvents => ServiceLocator.Current.GetInstance<IContentEvents>();
        protected virtual IPageMovedHandler PageMovedHandler => ServiceLocator.Current.GetInstance<IPageMovedHandler>();

        public virtual void Initialize(InitializationEngine context)
        {
            if (IsEnabledInConfig())
            {
                ContentEvents.MovingContent += ContentEventsOnMovingContent; 
                ContentEvents.MovedContent += ContentEventsOnMovedContent;
            }
        }

        protected virtual void ContentEventsOnMovedContent(object sender, ContentEventArgs e)
        {
            PageMovedHandler.ContentEventsOnMovedContent(sender, e);
        }

        protected virtual void ContentEventsOnMovingContent(object sender, ContentEventArgs e)
        {
            PageMovedHandler.ContentEventsOnMovingContent(sender, e);
        }

        protected virtual bool IsEnabledInConfig()
        {
            var result = (ConfigurationManager.AppSettings["CreateRedirectsOnMove"] ?? "false").Equals(bool.TrueString,
                             StringComparison.InvariantCultureIgnoreCase);
            return result;
        }

        public virtual void Uninitialize(InitializationEngine context)
        {
        }

        public virtual void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Container.Configure(x => x.For<IPageMovedHandler>().Singleton().Use<PageMovedHandler>());
        }
    }
}