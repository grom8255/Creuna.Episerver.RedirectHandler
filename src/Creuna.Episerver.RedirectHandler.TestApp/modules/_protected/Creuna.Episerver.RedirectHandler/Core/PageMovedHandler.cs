using EPiServer.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPiServer.Framework.Initialization;
using Microsoft.Azure;
using EPiServer.ServiceLocation;
using EPiServer.Core;
using EPiServer;
using EPiServer.Web.Routing;
using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;
using EPiServer.Web;
using System.Globalization;
using EPiServer.DataAbstraction;

namespace Creuna.Episerver.RedirectHandler.Core
{
    [InitializableModule]
    [ModuleDependency(typeof(ServiceContainerInitialization))]

    public class PageMovedHandler : IInitializableModule
    {
        UrlResolver _urlResolver;
        IContentEvents _contentEvents;
        CustomRedirectHandler _customRedirectHandler;
        ILanguageBranchRepository _languageBranchRepository;
        IContentRepository _contentRepository;
        const string PreviousUriKey = "PreviousLink";

        private UrlResolver UrlResolver
        {
            get
            {
                return _urlResolver ?? (_urlResolver = ServiceLocator.Current.GetInstance<UrlResolver>());
            }
        }

        private CustomRedirectHandler CustomRedirectHandler
        {
            get
            {
                return _customRedirectHandler ?? (_customRedirectHandler = ServiceLocator.Current.GetInstance<CustomRedirectHandler>());
            }
        }

        public void Initialize(InitializationEngine context)
        {
            if (IsEnabledInConfig())
            {
                if (_contentEvents == null)
                {
                    _contentEvents = ServiceLocator.Current.GetInstance<IContentEvents>();
                    _contentEvents.MovingContent += Events_MovingContent;
                    _contentEvents.MovedContent += MovedContent;
                }
            }
            _languageBranchRepository = ServiceLocator.Current.GetInstance<ILanguageBranchRepository>();
            _contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
        }

        private static bool IsEnabledInConfig()
        {
            return bool.Parse(CloudConfigurationManager.GetSetting("CreateRedirectsOnMove") ?? "False");
        }

        private void Events_MovingContent(object sender, ContentEventArgs e)
        {
            if (e.Content is PageData)
                e.Items.Add(PreviousUriKey, GetLanguageUrisFor(e.Content));
        }

        private Dictionary<CultureInfo, string> GetLanguageUrisFor(IContent content)
        {
            return _languageBranchRepository.ListEnabled()
                .Select(language => _contentRepository.Get<PageData>(content.ContentLink, language.Culture))
                .Where(p => p != null)
                .ToDictionary(p => p.Language, p => UrlResolver.GetUrl(p.ContentLink));
        }

        private void MovedContent(object sender, ContentEventArgs e)
        {
            if (e.Items.Contains(PreviousUriKey))
                CreateRedirect(e.Items[PreviousUriKey] as Dictionary<CultureInfo, string>, e.Content);
        }

        private void CreateRedirect(Dictionary<CultureInfo, string> previousUris, IContent content)
        {
            if (previousUris != null && previousUris.Any())
            {
                var redirects = new CustomRedirectCollection();
                foreach (var culture in previousUris.Keys)
                {
                    var previousUri = previousUris[culture];
                    CustomRedirect redirect = CreateRedirect(_urlResolver.GetUrl(content), culture, previousUri);
                    redirects.Add(redirect);

                    AddRedirectsForChildren(redirects, previousUri, culture, _urlResolver.GetVirtualPath(content.ContentLink).VirtualPath, _contentRepository.GetDescendents(content.ContentLink));
                }
                CustomRedirectHandler.SaveCustomRedirects(redirects);
            }
        }

        private CustomRedirect CreateRedirect(string newUri, CultureInfo culture, string previousUri)
        {
            return new CustomRedirect(
                                        BuildPathFromUri(previousUri, culture.Name).AbsoluteUri,
                                        BuildPathFromUri(newUri, culture.Name).AbsoluteUri,
                                        false, false, true);
        }

        private void AddRedirectsForChildren(CustomRedirectCollection redirects, string previousUri, CultureInfo culture, string newUri, IEnumerable<ContentReference> descendents)
        {
            foreach (var child in descendents)
            {
                redirects.Add(
                    CreateRedirect(
                        _urlResolver.GetUrl(child),
                        culture,
                        previousUri + _urlResolver.GetUrl(child).Substring(newUri.Length + 1)));
            }
        }

        public static Uri BuildPathFromUri(string path, string language)
        {
            return new Uri(GetBaseUriFor(language), path);
        }

        public static Uri GetBaseUriFor(string language)
        {
            var site = SiteDefinition.Current.GetPrimaryHost(new CultureInfo(language))
                ?? SiteDefinition.Current.GetPrimaryHost(null);
            var hostname = site.Name;
            var protocol = site.UseSecureConnection.GetValueOrDefault(false) ? "https" : "http";
            return new Uri(string.Concat(protocol, "://", hostname));
        }

        public void Uninitialize(InitializationEngine context)
        {
            throw new NotImplementedException();
        }
    }
}
