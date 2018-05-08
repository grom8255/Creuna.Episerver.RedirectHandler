using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Linq;
using Creuna.Episerver.RedirectHandler.Core.Data;
using EPiServer.Logging.Compatibility;

namespace Creuna.Episerver.RedirectHandler.Core.CustomRedirects
{
    /// <summary>
    ///     A collection of custom urls
    /// </summary>
    public class CustomRedirectCollection : CollectionBase
    {
        /// <summary>
        /// Hashtable for quick lookup of old urls
        /// </summary>
        private readonly Dictionary<string, CustomRedirect> _quickLookupTable = new Dictionary<string, CustomRedirect>();

        /*[CanBeNull]*/
        private string GetLookupKey(/*[NotNull]*/ CustomRedirect customRedirect)
        {
            if (customRedirect == null) throw new ArgumentNullException(nameof(customRedirect));
            return GetLookupKey(customRedirect.OldUrl);
        }

        /*[CanBeNull]*/
        private string GetLookupKey(/*[CanBeNull]*/ string url)
        {
            return UrlStandardizer.Standardize(url);
        }

        private void AddLookup(/*[NotNull]*/ CustomRedirect customRedirect)
        {
            if (customRedirect == null) throw new ArgumentNullException(nameof(customRedirect));

            _quickLookupTable[GetLookupKey(customRedirect)] = customRedirect;
        }

        private void RemoveLookup(/*[NotNull]*/ CustomRedirect customRedirect)
        {
            _quickLookupTable.Remove(GetLookupKey(customRedirect));
        }

        /*[CanBeNull]*/
        private CustomRedirect TryLookup(/*[NotNull]*/ string url)
        {
            CustomRedirect foundRedirect = null;
            _quickLookupTable.TryGetValue(GetLookupKey(url), out foundRedirect);
            return foundRedirect;
        }

        public int Add(CustomRedirect customRedirect)
        {
            // Add to quick look up table too
            AddLookup(customRedirect);
            return List.Add(customRedirect);
        }

        public int IndexOf(CustomRedirect customRedirect)
        {
            for (var i = 0; i < List.Count; i++)
            {
                if (this[i] == customRedirect) return i; // Found
            }
            return -1;
        }

        public void Insert(int index, CustomRedirect customRedirect)
        {
            AddLookup(customRedirect);
            List.Insert(index, customRedirect);
        }

        public void Remove(CustomRedirect customRedirect)
        {
            RemoveLookup(customRedirect);
            List.Remove(customRedirect);
        }

        /*[CanBeNull]*/
        public CustomRedirect Find(/*[NotNull]*/ Uri urlNotFound)
        {
            if (urlNotFound == null) throw new ArgumentNullException(nameof(urlNotFound));

            var urlsToSearch = new[]
            {
                // Handle absolute addresses first
                urlNotFound.AbsoluteUri,
                // and its protocol invariant version
                RemoveProtocol(urlNotFound.AbsoluteUri),
                // then try it without query
                RemoveQuery(urlNotFound.AbsoluteUri),
                // and the protocol invariant version
                RemoveProtocol(RemoveQuery(urlNotFound.AbsoluteUri)),
                // common case 
                urlNotFound.PathAndQuery,
                // and the same without query
                RemoveQuery(urlNotFound.PathAndQuery),
                // Handle legacy databases with encoded values
                HttpUtility.HtmlEncode(urlNotFound.PathAndQuery),
                // and the same without query
                HttpUtility.HtmlEncode(RemoveQuery(urlNotFound.PathAndQuery))
            };

            var foundRedirect = TryFind(urlsToSearch);
            var result = PostProcessRedirect(urlNotFound, foundRedirect);
            return result;
        }

        /*[NotNull]*/
        string RemoveProtocol(/*[NotNull]*/ string url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));

            const string http = "http:";
            if (url.StartsWith(http))
                return url.Substring(http.Length);

            const string https = "https:";
            if (url.StartsWith(https))
                return url.Substring(https.Length);

            return url;
        }

        /*[NotNull]*/
        string RemoveQuery(/*[NotNull]*/ string url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            var queryStartsAt = url.IndexOf("?", StringComparison.InvariantCultureIgnoreCase);

            var result = queryStartsAt > -1 ? url.Substring(0, queryStartsAt) : url;
            return result;
        }

        /*[CanBeNull]*/
        private CustomRedirect TryFind(/*[CanBeNull]*/ params string[] urls)
        {
            foreach (var url in urls.Distinct(StringComparer.InvariantCultureIgnoreCase))
            {
                var foundRedirect = FindInternal(url);
                if (foundRedirect != null)
                    return foundRedirect;
            }

            return null;
        }

        /*[CanBeNull]*/
        private CustomRedirect PostProcessRedirect(/*[NotNull]*/Uri urlNotFound, /*[CanBeNull]*/ CustomRedirect redirect)
        {
            if (urlNotFound == null) throw new ArgumentNullException(nameof(urlNotFound));
            if (redirect == null)
                return null;

            if (redirect.ExactMatch)
            {
                if (!string.IsNullOrEmpty(urlNotFound.Query))
                {
                    if (redirect.IncludeQueryString)
                    {
                        var newUrlParts = redirect.NewUrl.Split(new[] { "?" }, StringSplitOptions.RemoveEmptyEntries);
                        var newUrlQuery = newUrlParts.Length > 1 ? newUrlParts[1] : null;

                        var originalQueryParsed = HttpUtility.ParseQueryString(urlNotFound.Query);
                        var targetQueryParsed = newUrlQuery != null
                            ? HttpUtility.ParseQueryString(newUrlQuery)
                            : new NameValueCollection();

                        var appendQuery = Merge(targetQueryParsed, originalQueryParsed);
                        var newUrl = redirect.NewUrl;

                        redirect = new CustomRedirect(redirect)
                        {
                            NewUrl = newUrl.Contains("?") ? $"{newUrl}&{appendQuery}" : $"{newUrl}?{appendQuery}"
                        };
                    }
                }
            }
            else
            {
                if (redirect.AppendMatchToNewUrl)
                {
                    //    // We need to append the 404 to the end of the
                    //    // new one. Make a copy of the redir object as we
                    //    // are changing it.
                    var url = urlNotFound.ToString();
                    CustomRedirect redirCopy = new CustomRedirect(redirect);
                    var urlFromRule = UrlStandardizer.Standardize(redirect.OldUrl);

                    var append = IsAbsoluteUrl(urlFromRule) ? url.Substring(urlFromRule.Length) : urlNotFound.PathAndQuery.Substring(urlFromRule.Length);

                    if (append != string.Empty)
                    {
                        redirCopy.NewUrl = UrlStandardizer.Standardize(redirCopy.NewUrl + append);
                        return redirCopy;
                    }
                }
            }

            return redirect;
        }

        private bool IsAbsoluteUrl(/*[NotNull]*/ string url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));

            return url.StartsWith("//", StringComparison.InvariantCultureIgnoreCase)
                   || url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)
                   || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase);
        }

        /*[NotNull]*/
        private string Merge(/*[NotNull]*/ NameValueCollection targetQuery, /*[NotNull]*/ NameValueCollection originalQuery)
        {
            if (targetQuery == null) throw new ArgumentNullException(nameof(targetQuery));
            if (originalQuery == null) throw new ArgumentNullException(nameof(originalQuery));

            var appendQueryArray = originalQuery.AllKeys.Where(x => targetQuery[x] == null)
                .Select(x => $"{HttpUtility.UrlEncode(x)}={HttpUtility.UrlEncode(originalQuery[x])}").ToArray();
            var query = string.Join("&", appendQueryArray);
            return query;
        }

        private CustomRedirect FindInternal(string url)
        {
            var foundRedirect = TryLookup(url);

            if (foundRedirect != null)
            {
                return foundRedirect;
            }

            // No exact match could be done, so we'll check if the 404 url
            // starts with one of the urls we're matching against. This
            // will be kind of a wild card match (even though we only check
            // for the start of the url
            // Example: http://www.mysite.com/news/mynews.html is not found
            // We have defined an "<old>/news</old>" entry in the config
            // file. We will get a match on the /news part of /news/myne...
            // Depending on the skip wild card append setting, we will either
            // redirect using the <new> url as is, or we'll append the 404
            // url to the <new> url.

            foreach (KeyValuePair<string, CustomRedirect> pair in _quickLookupTable)
            {
                var redirect = pair.Value;
                if (redirect.ExactMatch)
                    continue; // todo: [low] performance issue: we can store only non-exact-mathces separately
                // See if this "old" url (the one that cannot be found) starts with one 
                if (url.StartsWith(pair.Key, StringComparison.InvariantCultureIgnoreCase))
                {
                    foundRedirect = redirect;
                    if (redirect.State == GetState.Ignored)
                    {
                        return null;
                    }

                    return redirect;
                    //if (redirect.WildCardSkipAppend == true)
                    //{
                    //    // We'll redirect without appending the 404 url
                    //    return redirect;
                    //}
                    //else
                    //{
                    //    return redirect;
                    //    // We need to append the 404 to the end of the
                    //    // new one. Make a copy of the redir object as we
                    //    // are changing it.
                    //    CustomRedirect redirCopy = new CustomRedirect(redirect);
                    //    redirCopy.NewUrl = redirCopy.NewUrl + url.Substring(pair.Key.Length);
                    //    return redirCopy;
                    //}
                }
            }
            return null;
        }

        public bool Contains(string oldUrl)
        {
            return _quickLookupTable.ContainsKey(GetLookupKey(oldUrl));
        }

        public CustomRedirect this[int index]
        {
            get => (CustomRedirect)List[index];
            set => List[index] = value;
        }

        ////private static readonly ILog Log = LogManager.GetLogger(typeof(CustomRedirectCollection));

        /////// <summary>
        ///////     Hashtable for quick lookup of old urls
        /////// </summary>
        ////private readonly Dictionary<string, CustomRedirect> _quickLookupTable;

        ////public CustomRedirectCollection()
        ////{
        ////    // Create case insensitive hash table
        ////    _quickLookupTable = new Dictionary<string, CustomRedirect>(StringComparer.InvariantCultureIgnoreCase);
        ////}

        ////#region Add

        ////public int Add(CustomRedirect customRedirect)
        ////{
        ////    //var oldUrl = HttpUtility.UrlDecode(customRedirect.OldUrl);
        ////    //if (_quickLookupTable.ContainsKey(oldUrl))
        ////    //    Log.WarnFormat("Two or more redirects set up for Old Url: {0}", customRedirect.OldUrl);
        ////    //else
        ////    //    _quickLookupTable.Add(oldUrl, customRedirect);
        ////    //return List.Add(customRedirect);

        ////    // Add to quick look up table too
        ////    AddLookup(customRedirect);
        ////    return List.Add(customRedirect);
        ////}

        ////public int IndexOf(CustomRedirect customRedirect)
        ////{
        ////    for (var i = 0; i < List.Count; i++)
        ////    {
        ////        if (this[i] == customRedirect) return i; // Found
        ////    }
        ////    return -1;
        ////}

        ////#endregion

        ////#region Insert

        ////public void Insert(int index, CustomRedirect customRedirect)
        ////{
        ////    _quickLookupTable.Add(customRedirect.OldUrl, customRedirect);
        ////    List.Insert(index, customRedirect);
        ////}

        ////#endregion

        ////#region Remove

        ////public void Remove(CustomRedirect customRedirect)
        ////{
        ////    _quickLookupTable.Remove(customRedirect.OldUrl);
        ////    List.Remove(customRedirect);
        ////}

        ////#endregion

        ////#region Find

        ////// TODO: If desired, change parameters to Find method to search based on a property of CustomRedirect.
        ////public CustomRedirect Find(Uri urlNotFound)
        ////{
        ////    if (!urlNotFound.IsAbsoluteUri)
        ////    {
        ////        return null;
        ////    }

        ////    var result = FindFromAbsoluteUrl(urlNotFound);

        ////    if (result == null)
        ////    {
        ////        var urlNotFoundWithoutQuery = RemoveQuerystringFrom(urlNotFound);
        ////        result = FindFromAbsoluteUrl(urlNotFoundWithoutQuery, urlNotFound.Query);
        ////    }

        ////    return result;
        ////}

        ////private Uri RemoveQuerystringFrom(Uri urlNotFound)
        ////{
        ////    return new Uri(urlNotFound.GetLeftPart(UriPartial.Path));
        ////}

        ////private CustomRedirect FindFromAbsoluteUrl(Uri urlNotFound, string queryString = null)
        ////{
        ////    return FindRedirect(urlNotFound, Uri.UnescapeDataString(urlNotFound.PathAndQuery), queryString)
        ////           ?? FindRedirect(urlNotFound, urlNotFound.AbsolutePath, queryString);
        ////}

        ////private CustomRedirect FindRedirect(Uri urlNotFound, string oldUri, string queryString = null)
        ////{
        ////    if (queryString == null)
        ////    {
        ////        queryString = urlNotFound.Query;
        ////    }

        ////    CustomRedirect redirect = null;
        ////    var absoluteUri = HttpUtility.UrlDecode(urlNotFound.AbsoluteUri);
        ////    if (_quickLookupTable.ContainsKey(absoluteUri))
        ////        redirect = BuildNewUrl(_quickLookupTable[absoluteUri], absoluteUri, oldUri, queryString);
        ////    if (_quickLookupTable.ContainsKey(oldUri))
        ////        redirect = BuildNewUrl(_quickLookupTable[oldUri], absoluteUri, oldUri, queryString);
        ////    if (redirect != null)
        ////        return redirect;

        ////    // No exact match could be done, so we'll check if the 404 url
        ////    // starts with one of the urls we're matching against. This
        ////    // will be kind of a wild card match (even though we only check
        ////    // for the start of the url
        ////    // Example: http://www.mysite.com/news/mynews.html is not found
        ////    // We have defined an "<old>/news</old>" entry in the config
        ////    // file. We will get a match on the /news part of /news/myne...
        ////    // Depending on the skip wild card append setting, we will either
        ////    // redirect using the <new> url as is, or we'll append the 404
        ////    // url to the <new> url.

        ////    return _quickLookupTable.Keys
        ////        .Where(k => oldUri.StartsWith(k, StringComparison.InvariantCultureIgnoreCase))
        ////        .Select(key => Tuple.Create(key, _quickLookupTable[key]))
        ////        .Where(r => !r.Item2.ExactMatch)
        ////        .Select(r => BuildNewUrl(r.Item2, r.Item1, oldUri, urlNotFound.Query))
        ////        .FirstOrDefault();
        ////}

        ////private CustomRedirect BuildNewUrl(
        ////    CustomRedirect customRedirect,
        ////    string oldUri,
        ////    string absolutePath,
        ////    string querystring)
        ////{
        ////    var newUrl = customRedirect.AppendMatchToNewUrl
        ////        ? AppendMatch(customRedirect, absolutePath)
        ////        : customRedirect.NewUrl;

        ////    if (customRedirect.IncludeQueryString && querystring.Length > 1)
        ////        newUrl = CreateUrlWithQuerystring(querystring, newUrl);
        ////    return customRedirect.WithNewUrl(newUrl);
        ////}

        ////private static string AppendMatch(CustomRedirect customRedirect, string absolutePath)
        ////{
        ////    var newUrl = new Uri(customRedirect.NewUrl, UriKind.RelativeOrAbsolute);
        ////    var oldUri = new Uri(absolutePath, UriKind.RelativeOrAbsolute);

        ////    var path = GetPathFromLocalUri(oldUri);

        ////    var oldUrlLength = customRedirect.OldUrl.Length;

        ////    string uriToAppend;

        ////    if (path.Length < oldUrlLength)
        ////    {
        ////        uriToAppend = path;
        ////    }
        ////    else
        ////    {
        ////        uriToAppend = path.Substring(oldUrlLength);
        ////    }

        ////    var querystring = GetQueryFrom(newUrl);
        ////    return CombineUri(newUrl, uriToAppend) + (string.IsNullOrWhiteSpace(querystring) ? string.Empty : string.Concat("?", querystring));
        ////}

        ////public static string GetOnlyPathFrom(Uri oldUri)
        ////{
        ////    if (oldUri.IsAbsoluteUri)
        ////        return oldUri.PathAndQuery;
        ////    return GetPathFromLocalUri(oldUri);
        ////}

        ////private static string CombineUri(Uri newUrl, string uriToAppend)
        ////{
        ////    var path = GetPathFrom(newUrl);
        ////    return path.EndsWith("/", StringComparison.OrdinalIgnoreCase) && uriToAppend.StartsWith("/", StringComparison.OrdinalIgnoreCase)
        ////        ? path + uriToAppend.Substring(1) : path + uriToAppend;
        ////}

        ////private static string GetPathFrom(Uri newUrl)
        ////{
        ////    return (newUrl.IsAbsoluteUri ? newUrl.GetLeftPart(UriPartial.Path) : GetPathFromLocalUri(newUrl));
        ////}

        ////private static string GetPathFromLocalUri(Uri localUrl)
        ////{
        ////    var queryStart = localUrl.OriginalString.IndexOf('?');
        ////    if (queryStart > -1)
        ////        return localUrl.OriginalString.Substring(0, queryStart);
        ////    return localUrl.OriginalString;
        ////}

        ////private static string GetQueryFrom(Uri uri)
        ////{
        ////    return uri.IsAbsoluteUri ? GetQuerystringWithoutQuestionMark(uri.Query) : GetQuerystringWithoutQuestionMark(GetQueryFromLocalUri(uri));
        ////}

        ////private static string GetQueryFromLocalUri(Uri localUri)
        ////{
        ////    var queryStart = localUri.OriginalString.IndexOf('?');
        ////    if (queryStart > -1)
        ////        return localUri.OriginalString.Substring(queryStart + 1);
        ////    return string.Empty;
        ////}

        ////private static string CreateUrlWithQuerystring(string querystring, string newUrl)
        ////{
        ////    if (newUrl.IndexOf("?", StringComparison.OrdinalIgnoreCase) > -1)
        ////        return string.Concat(newUrl, "&", GetQuerystringWithoutQuestionMark(querystring));
        ////    return string.Concat(newUrl, "?", GetQuerystringWithoutQuestionMark(querystring));
        ////}

        ////private static string GetQuerystringWithoutQuestionMark(string querystring)
        ////{
        ////    return querystring.StartsWith("?", StringComparison.OrdinalIgnoreCase) ? querystring.Substring(1) : querystring;
        ////}

        ////#endregion

        ////#region Contains

        ////// TODO: If you changed the parameters to Find (above), change them here as well.
        ////public bool Contains(string oldUrl)
        ////{
        ////    return _quickLookupTable.ContainsKey(oldUrl);
        ////}

        ////#endregion

        ////#region this[int aIndex]

        ////public CustomRedirect this[int index]
        ////{
        ////    get { return (CustomRedirect)List[index]; }
        ////    set { List[index] = value; }
        ////}

        ////#endregion

        ////#region Vladimir's fixes

        ////private void AddLookup(/*[NotNull]*/ CustomRedirect customRedirect)
        ////{
        ////    if (customRedirect == null) throw new ArgumentNullException(nameof(customRedirect));

        ////    _quickLookupTable[GetLookupKey(customRedirect)] = customRedirect;
        ////}

        ////private void RemoveLookup(/*[NotNull]*/ CustomRedirect customRedirect)
        ////{
        ////    _quickLookupTable.Remove(GetLookupKey(customRedirect));
        ////}

        /////*[CanBeNull]*/
        ////private CustomRedirect TryLookup(/*[NotNull]*/ string url)
        ////{
        ////    CustomRedirect foundRedirect = null;
        ////    _quickLookupTable.TryGetValue(GetLookupKey(url), out foundRedirect);
        ////    return foundRedirect;
        ////}

        ////private string GetLookupKey(/*[NotNull]*/ CustomRedirect customRedirect)
        ////{
        ////    if (customRedirect == null) throw new ArgumentNullException(nameof(customRedirect));
        ////    return GetLookupKey(customRedirect.OldUrl);
        ////}

        /////*[CanBeNull]*/
        ////private string GetLookupKey(/*[CanBeNull]*/ string url)
        ////{
        ////    return UrlStandardizer.Standardize(url);
        ////}

        ////#endregion
    }
}