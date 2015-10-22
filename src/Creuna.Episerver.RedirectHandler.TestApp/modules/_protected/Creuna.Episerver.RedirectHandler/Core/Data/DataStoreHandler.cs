using System.Collections.Generic;
using System.Linq;
using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;
using EPiServer.Data.Dynamic;

namespace Creuna.Episerver.RedirectHandler.Core.Data
{
    public class DataStoreHandler
    {
        public enum GetState
        {
            Saved = 0,
            Suggestion = 1,
            Ignored = 2
        };

        private const string OldUrlPropertyName = "OldUrl";

        public void SaveCustomRedirect(CustomRedirect currentCustomRedirect)
        {
            // Get hold of the datastore
            using (DynamicDataStore store = DataStoreFactory.GetStore(typeof(CustomRedirect)))
            {
                //check if there is an exisiting object with matching property "OldUrl"
                CustomRedirect match =
                    store.Find<CustomRedirect>(OldUrlPropertyName, currentCustomRedirect.OldUrl.ToLower())
                        .SingleOrDefault();
                //if there is a match, replace the value.
                if (match != null)
                    store.Save(currentCustomRedirect, match.Id);
                else
                    store.Save(currentCustomRedirect);
            }
        }


        /// <summary>
        ///     Returns a list of all CustomRedirect objects in the Dynamic Data Store.
        /// </summary>
        /// <returns></returns>
        public List<CustomRedirect> GetCustomRedirects(bool excludeIgnored)
        {
            using (var store = DataStoreFactory.GetStore(typeof(CustomRedirect)))
            {
                return
                    store.Items<CustomRedirect>()
                        .Where(r => (!excludeIgnored) || r.State.Equals((int)GetState.Saved))
                        .ToList();
            }
        }

        public List<CustomRedirect> GetIgnoredRedirect()
        {
            using (var store = DataStoreFactory.GetStore(typeof(CustomRedirect)))
            {
                IQueryable<CustomRedirect> customRedirects =
                    from s in store.Items<CustomRedirect>().OrderBy(cr => cr.OldUrl)
                    where s.State.Equals(GetState.Ignored)
                    select s;
                return customRedirects.ToList();
            }
        }

        public void UnignoreRedirect()
        {
            // TODO
        }


        /// <summary>
        ///     Delete CustomObject object from Data Store that has given "OldUrl" property
        /// </summary>
        /// <param name="oldUrl"></param>
        public void DeleteCustomRedirect(string oldUrl)
        {
            // Get hold of the datastore
            using (var store = DataStoreFactory.GetStore(typeof(CustomRedirect)))
            {
                //find object with matching property "OldUrl"
                CustomRedirect match =
                    store.Find<CustomRedirect>(OldUrlPropertyName, oldUrl.ToLower()).SingleOrDefault();
                if (match != null)
                    store.Delete(match);
            }
        }

        /// <summary>
        ///     Delete all CustomRedirect objects
        /// </summary>
        public void DeleteAllCustomRedirects()
        {
            // In order to avoid a database timeout, we delete the items one by one.
            using (var store = DataStoreFactory.GetStore(typeof(CustomRedirect)))
            {
                foreach (CustomRedirect redirect in GetCustomRedirects(false))
                {
                    store.Delete(redirect);
                }
            }
        }

        public int DeleteAllIgnoredRedirects()
        {
            // In order to avoid a database timeout, we delete the items one by one.
            using (var store = DataStoreFactory.GetStore(typeof(CustomRedirect)))
            {
                List<CustomRedirect> ignoredRedirects = GetIgnoredRedirect();
                foreach (CustomRedirect redirect in ignoredRedirects)
                {
                    store.Delete(redirect);
                }
                return ignoredRedirects.Count();
            }
        }


        /// <summary>
        ///     Find all CustomRedirect objects which has a OldUrl og NewUrl that contains the search word.
        /// </summary>
        /// <param name="searchWord"></param>
        /// <returns></returns>
        public List<CustomRedirect> SearchCustomRedirects(string searchWord)
        {
            using (var store = DataStoreFactory.GetStore(typeof(CustomRedirect)))
            {
                return (from s in store.Items<CustomRedirect>()
                        where s.NewUrl.Contains(searchWord) || s.OldUrl.Contains(searchWord)
                        select s).ToList();
            }
        }
    }
}