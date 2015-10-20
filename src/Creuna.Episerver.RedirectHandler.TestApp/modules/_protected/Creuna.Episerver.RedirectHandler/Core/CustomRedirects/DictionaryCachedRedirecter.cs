using System;
using System.Collections.Generic;

namespace Creuna.Episerver.RedirectHandler.Core.CustomRedirects
{
    public class DictionaryCachedRedirecter : ICachedRedirecter
    {
        private readonly IRedirecter _redirecter;
        private readonly Dictionary<string, RedirectAttempt> _redirects
            = new Dictionary<string, RedirectAttempt>(StringComparer.OrdinalIgnoreCase);
        private readonly object _lockObject = new object();

        public DictionaryCachedRedirecter(IRedirecter redirecter)
        {
            _redirecter = redirecter;
        }

        public RedirectAttempt Redirect(string referer, Uri urlNotFound)
        {
            if (!_redirects.ContainsKey(urlNotFound.PathAndQuery))
            {
                lock (_lockObject)
                {
                    if (!_redirects.ContainsKey(urlNotFound.PathAndQuery))
                    {
                        _redirects.Add(urlNotFound.PathAndQuery, _redirecter.Redirect(referer, urlNotFound));
                    }
                }
            }
            return _redirects[urlNotFound.PathAndQuery];
        }

        public long GetCachedRedirectsCount()
        {
            return _redirects.Count;
        }

        public void ClearCache()
        {
            _redirects.Clear();
        }
    }
}
