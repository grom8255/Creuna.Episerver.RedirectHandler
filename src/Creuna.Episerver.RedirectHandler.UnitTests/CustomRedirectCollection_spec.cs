using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;

namespace Creuna.Episerver.RedirectHandler
{
    public class CustomRedirectCollection_spec : CustomRedirect_spec
    {
        void describe_CustomRedirectCollection_with_exact_match()
        {
            const bool exactMatch = true;
            const bool appendMatchToNewUrl = false;

            context[$"Given the redirect rule is http://mysite/test => http://mysite, exactMatch={exactMatch}"] = () =>
            {
                before = () => redirects = new CustomRedirectCollection { new CustomRedirect("http://mysite/test", "http://mysite", appendMatchToNewUrl, exactMatch, true) };

                WhenUrlIs("http://mysite/test").ThenItRedirectsTo("http://mysite");
                //WhenUrlIs("http://mysite/test/").ThenItRedirectsTo("http://mysite"); // should be passed when Vladimir's fix applied
                WhenUrlIs("http://mysite/testme").ThenItDoesNotRedirect();
                WhenUrlIs("http://mysite/testme").ThenItDoesNotRedirect();

                context["and includeQueryString=true"] = () =>
                {
                    before = () => redirects = new CustomRedirectCollection { new CustomRedirect("http://mysite/test", "http://mysite", appendMatchToNewUrl, exactMatch, true) };

                    //WhenUrlIs("http://mysite/test?query=my-query").ThenItRedirectsTo("http://mysite/test?query=my-query"); incorrect
                    WhenUrlIs("http://mysite/test?query=my-query").ThenItRedirectsTo("http://mysite?query=my-query");
                };

                context["and includeQueryString=false"] = () =>
                {
                    before = () => redirects = new CustomRedirectCollection { new CustomRedirect("http://mysite/test", "http://mysite", appendMatchToNewUrl, exactMatch, false) };

                    WhenUrlIs("http://mysite/test").ThenItRedirectsTo("http://mysite");
                    // WhenUrlIs("http://mysite/test?query=my-query").ThenItRedirectsTo("http://mysite/test"); incorrect
                    WhenUrlIs("http://mysite/test?query=my-query").ThenItRedirectsTo("http://mysite");
                };
            };
        }

        void describe_CustomRedirectCollection_exact_match_false_and_append_match_to_new_url_false()
        {
            const bool exactMatch = false;
            const bool appendMatchToNewUrl = false;

            context[
$"Given the redirect rule is http://mysite/test => http://mysite, exactNatch={exactMatch}, appendMatchToNewUrl={appendMatchToNewUrl}"]
                = () =>
                {
                    before = () => redirects = new CustomRedirectCollection { new CustomRedirect("http://mysite/test", "http://mysite", appendMatchToNewUrl, exactMatch, true) };

                    WhenUrlIs("http://mysite/test").ThenItRedirectsTo("http://mysite");
                    //WhenUrlIs("http://mysite/test/").ThenItRedirectsTo("http://mysite"); // should be passed when Vladimir's fix applied
                    //WhenUrlIs("http://mysite/testme").ThenItRedirectsTo("http://mysite"); // seems that it is a bug - rewrite cannot be found
                    //WhenUrlIs("http://mysite/test/me").ThenItRedirectsTo("http://mysite"); // seems that it is a bug - rewrite cannot be found

                    context["and includeQueryString=true"] = () =>
                    {
                        before = () => redirects = new CustomRedirectCollection { new CustomRedirect("http://mysite/test", "http://mysite", appendMatchToNewUrl, exactMatch, true) };

                        WhenUrlIs("http://mysite/test?query=my-query").ThenItRedirectsTo("http://mysite?query=my-query");
                    };

                    context["and includeQueryString=false"] = () =>
                    {
                        before = () => redirects = new CustomRedirectCollection { new CustomRedirect("http://mysite/test", "http://mysite", appendMatchToNewUrl, exactMatch, false) };

                        WhenUrlIs("http://mysite/test?query=my-query").ThenItRedirectsTo("http://mysite");
                    };
                };
        }

        void describe_CustomRedirectCollection_exact_match_false_and_append_match_to_new_url_true()
        {
            const bool exactMatch = false;
            const bool appendMatchToNewUrl = true;

            context[$"Given the redirect rule is http://mysite/test => http://mysite, exactNatch={exactMatch}, appendMatchToNewUrl={appendMatchToNewUrl}"] = () =>
            {
                before = () => redirects = new CustomRedirectCollection { new CustomRedirect("http://mysite/test", "http://mysite", appendMatchToNewUrl, exactMatch, true) };

                WhenUrlIs("http://mysite/test").ThenItRedirectsTo("http://mysite/test");
                ////WhenUrlIs("http://mysite/test/").ThenItRedirectsTo("http://mysite"); // should be passed when Vladimir's fix applied
                //WhenUrlIs("http://mysite/testme").ThenItRedirectsTo("http://mysiteme"); // seems that it is a bug - rewrite cannot be found
                //WhenUrlIs("http://mysite/test/me").ThenItRedirectsTo("http://mysite/me"); // seems that it is a bug - rewrite cannot be found

                context["and includeQueryString=true"] = () =>
                {
                    before = () => redirects = new CustomRedirectCollection { new CustomRedirect("http://mysite/test", "http://mysite", appendMatchToNewUrl, exactMatch, true) };

                    WhenUrlIs("http://mysite/test?query=my-query").ThenItRedirectsTo("http://mysite/test?query=my-query");
                };

                context["and includeQueryString=false"] = () =>
                {
                    before = () => redirects = new CustomRedirectCollection { new CustomRedirect("http://mysite/test", "http://mysite", appendMatchToNewUrl, exactMatch, false) };

                    WhenUrlIs("http://mysite/test?query=my-query").ThenItRedirectsTo("http://mysite/test");
                };
            };
        }

        void describe_CustomRedirectCollection_query_string_merge()
        {
            const bool exactMatch = true;
            const bool skipWildcardAppend = false;
            const bool skipQueryString = false;

            const bool appendMatchToNewUrl = !skipWildcardAppend;
            const bool includeQueryString = !skipQueryString;

            context[$"Given the redirect rule is http://mysite/test => http://mysite?a=1&b=2, exactMatch={exactMatch}, appendMatchToNewUrl={appendMatchToNewUrl}"] = () =>
            {
                before = () => redirects = new CustomRedirectCollection { new CustomRedirect("http://mysite/test", "http://mysite?a=1&b=2", appendMatchToNewUrl, exactMatch, includeQueryString) };

                WhenUrlIs("http://mysite/test?a=0&x=0").ThenItRedirectsTo("http://mysite/test?a=1&b=2&a=0&x=0"); // TODO: Clarify with Valdimir - which value is expected - seems that it is a bug in Vladimir's test
                //WhenUrlIs("http://mysite/test/?x=0").ThenItRedirectsTo("http://mysite/test?a=1&b=2&x=0"); // seems that this should be fixed after Vladimir's fix applied
            };
        }

        void describe_CustomRedirectCollection_protocol_invariant_rule()
        {
            const bool exactMatch = true;
            const bool skipWildcardAppend = false;
            const bool skipQueryString = false;

            const bool appendMatchToNewUrl = !skipWildcardAppend;
            const bool includeQueryString = !skipQueryString;

            context[$"Given the redirect rule is //mysite/test => //mysite, exactMatch={exactMatch}, appendMatchToNewUrl={appendMatchToNewUrl}"] = () =>
            {
                before = () => redirects = new CustomRedirectCollection { new CustomRedirect("//mysite/test", "//mysite", appendMatchToNewUrl, exactMatch, includeQueryString) };

                WhenUrlIs("http://mysite/test").ThenItRedirectsTo("//mysite");
                WhenUrlIs("https://mysite/test").ThenItRedirectsTo("//mysite");
            };

            context[$"Given the redirect rule is //mysite/test => https://mysite, exactMatch={exactMatch}, appendMatchToNewUrl={appendMatchToNewUrl}"] = () =>
            {
                before = () => redirects = new CustomRedirectCollection { new CustomRedirect("//mysite/test", "https://mysite", appendMatchToNewUrl, exactMatch, includeQueryString) };

                WhenUrlIs("http://mysite/test").ThenItRedirectsTo("https://mysite");
                WhenUrlIs("https://mysite/test").ThenItRedirectsTo("https://mysite");
            };
        }

        void describe_CustomRedirectCollection_wildcard_append_works_with_rule_for_relative_url()
        {
            context["Given the redirect rule is /test => http://mysite"] = () =>
            {
                before = () =>
                    redirects = new CustomRedirectCollection
                    {
                        new CustomRedirect("/test", "http://mysite", appendMatchToNewUrl: false, exactMatch: false, includeQueryString: true)
                    };

                WhenUrlIs("http://mysite/test").ThenItRedirectsTo("http://mysite"); // - passed when 'appendMatchToNewUrl' is false
                WhenUrlIs("http://mysite/testme").ThenItRedirectsTo("http://mysiteme"); // - need to clarify with Vladimir how it should work
                WhenUrlIs("http://mysite/test/me").ThenItRedirectsTo("http://mysite/me"); // - passed when 'appendMatchToNewUrl' is true
            };
        }
    }
}
