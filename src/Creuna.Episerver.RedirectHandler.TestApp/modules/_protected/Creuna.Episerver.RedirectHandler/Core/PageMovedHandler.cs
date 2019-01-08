using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public interface IPageMovedHandler
    {
        void ContentEventsOnMovedContent(object sender, ContentEventArgs e);
        void ContentEventsOnMovingContent(object sender, ContentEventArgs e);
    }

    public class PageMovedHandler : IPageMovedHandler
    {
        const string PreviousUriKey = "PreviousLink";

        protected virtual UrlResolver UrlResolver => ServiceLocator.Current.GetInstance<UrlResolver>();

        protected virtual CustomRedirectHandler CustomRedirectHandler => ServiceLocator.Current.GetInstance<CustomRedirectHandler>();
        protected virtual IContentRepository ContentRepository => ServiceLocator.Current.GetInstance<IContentRepository>();
        protected virtual ILanguageBranchRepository LanguageBranchRepository => ServiceLocator.Current.GetInstance<ILanguageBranchRepository>();

        public virtual void ContentEventsOnMovingContent(object sender, ContentEventArgs e)
        {
            if (e.Content is PageData)
                e.Items.Add(PreviousUriKey, GetLanguageUrisFor(e.Content));
        }

        protected virtual Dictionary<CultureInfo, string> GetLanguageUrisFor(IContent content)
        {
            return LanguageBranchRepository.ListEnabled()
                .Select(language => ContentRepository.Get<PageData>(content.ContentLink, language.Culture))
                .Where(p => p != null)
                .ToDictionary(p => p.Language, p => UrlResolver.GetUrl(p.ContentLink));
        }

        public virtual void ContentEventsOnMovedContent(object sender, ContentEventArgs e)
        {
            if (e.Items.Contains(PreviousUriKey))
                CreateRedirect(e.Items[PreviousUriKey] as Dictionary<CultureInfo, string>, e.Content);
        }

        protected virtual void CreateRedirect(Dictionary<CultureInfo, string> previousUris, IContent content)
        {
            if (previousUris != null && previousUris.Any())
            {
                var redirects = new CustomRedirectCollection();
                foreach (var culture in previousUris.Keys)
                {
                    var previousUri = previousUris[culture];
                    CustomRedirect redirect = CreateRedirect(UrlResolver.GetUrl(content), culture, previousUri);
                    redirects.Add(redirect);

                    AddRedirectsForChildren(redirects, previousUri, culture, UrlResolver.GetVirtualPath(content.ContentLink).VirtualPath, ContentRepository.GetDescendents(content.ContentLink));
                }
                CustomRedirectHandler.SaveCustomRedirects(redirects);
            }
        }

        protected virtual CustomRedirect CreateRedirect(string newUri, CultureInfo culture, string previousUri)
        {
            return new CustomRedirect(
                                        BuildPathFromUri(previousUri, culture.Name).AbsoluteUri,
                                        BuildPathFromUri(newUri, culture.Name).AbsoluteUri,
                                        false, true, true);
        }

        protected virtual void AddRedirectsForChildren(CustomRedirectCollection redirects, string previousUri, CultureInfo culture, string newUri, IEnumerable<ContentReference> descendents)
        {
            foreach (var child in descendents)
            {
                redirects.Add(
                    CreateRedirect(
                        UrlResolver.GetUrl(child),
                        culture,
                        previousUri + UrlResolver.GetUrl(child).Substring(newUri.Length + 1)));
            }
        }

        protected virtual Uri BuildPathFromUri(string path, string language)
        {
            return new Uri(GetBaseUriFor(language), path);
        }

        protected virtual Uri GetBaseUriFor(string language)
        {
            var site = SiteDefinition.Current.GetPrimaryHost(new CultureInfo(language))
                ?? SiteDefinition.Current.GetPrimaryHost(null);
            var hostname = site.Name;
            var protocol = site.UseSecureConnection.GetValueOrDefault(false) ? "https" : "http";
            return new Uri(string.Concat(protocol, "://", hostname));
        }
    }
}
