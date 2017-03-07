using System.Collections.Generic;
using System.Linq;
using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;
using EPiServer.ServiceLocation;

namespace Creuna.Episerver.RedirectHandler.Core.Data
{
    public enum GetState
    {
        Saved = 0,
        Suggestion = 1,
        Ignored = 2
    };

    [ServiceConfiguration(typeof(DataStoreHandler), Lifecycle = ServiceInstanceScope.Singleton)]
    public class DataStoreHandler
    {
        public virtual void SaveCustomRedirect(CustomRedirect currentCustomRedirect)
        {
            using (var context = new CustomRedirectContext())
            {
                //check if there is an exisiting object with matching property "OldUrl"
                CustomRedirect match =
                    context.CustomRedirects.SingleOrDefault(r => r.OldUrl.Equals(currentCustomRedirect.OldUrl));
                //if there is a match, replace the value.
                if (match != null)
                    context.CustomRedirects.Remove(match);
                context.CustomRedirects.Add(currentCustomRedirect);
                context.SaveChanges();
            }
        }

        public ContextWrapper GetStore()
        {
            return new ContextWrapper();
        }

        /// <summary>
        ///     Returns a list of all CustomRedirect objects in the Dynamic Data Store.
        /// </summary>
        /// <returns></returns>
        public virtual List<CustomRedirect> GetCustomRedirects(bool excludeIgnored)
        {
            using (var context = new CustomRedirectContext())
            {
                IEnumerable<CustomRedirect> customRedirects;
                if (excludeIgnored)
                {
                    customRedirects = from s in context.CustomRedirects.OrderBy(cr => cr.OldUrl)
                                      where s.State == GetState.Saved
                                      select s;
                }
                else
                {
                    customRedirects = from s in context.CustomRedirects.OrderBy(cr => cr.OldUrl)
                                      select s;
                }
                return customRedirects.ToList();
            }
        }

        public virtual List<CustomRedirect> GetIgnoredRedirect()
        {
            using (var context = new CustomRedirectContext())
            {

                IQueryable<CustomRedirect> customRedirects =
                    from s in context.CustomRedirects.OrderBy(cr => cr.OldUrl)
                    where s.State == GetState.Ignored
                    select s;
                return customRedirects.ToList();
            }
        }

        private void UnignoreRedirect()
        {
            // TODO
        }

        /// <summary>
        ///     Delete CustomObject object from Data Store that has given "OldUrl" property
        /// </summary>
        /// <param name="oldUrl"></param>
        public virtual void DeleteCustomRedirect(string oldUrl)
        {
            // Get hold of the datastore
            using (var context = new CustomRedirectContext())
            {
                //find object with matching property "OldUrl"
                CustomRedirect match =
                    context.CustomRedirects.SingleOrDefault(r => r.OldUrl.Equals(oldUrl));
                if (match != null)
                    context.CustomRedirects.Remove(match);
                context.SaveChanges();
            }
        }

        /// <summary>
        ///     Delete all CustomRedirect objects
        /// </summary>
        public virtual void DeleteAllCustomRedirects()
        {
            // In order to avoid a database timeout, we delete the items one by one.
            using (var context = new CustomRedirectContext())
            {
                foreach (CustomRedirect redirect in GetCustomRedirects(false))
                {
                    context.CustomRedirects.Remove(redirect);
                }
                context.SaveChanges();
            }
        }

        public virtual int DeleteAllIgnoredRedirects()
        {
            // In order to avoid a database timeout, we delete the items one by one.
            using (var context = new CustomRedirectContext())
            {
                List<CustomRedirect> ignoredRedirects = GetIgnoredRedirect();
                foreach (CustomRedirect redirect in ignoredRedirects)
                {
                    context.CustomRedirects.Remove(redirect);
                }
                context.SaveChanges();
                return ignoredRedirects.Count();
            }
        }

        /// <summary>
        ///     Find all CustomRedirect objects which has a OldUrl og NewUrl that contains the search word.
        /// </summary>
        /// <param name="searchWord"></param>
        /// <returns></returns>
        public virtual List<CustomRedirect> SearchCustomRedirects(string searchWord)
        {
            using (var context = new CustomRedirectContext())
            {
                return (from s in context.CustomRedirects
                        where s.NewUrl.Contains(searchWord) || s.OldUrl.Contains(searchWord)
                        select s).OrderBy(cr => cr.OldUrl).ToList();
            }
        }
    }
}