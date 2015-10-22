﻿using System.Collections.Generic;
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

        private const string OLD_URL_PROPERTY_NAME = "OldUrl";

        public virtual void SaveCustomRedirect(CustomRedirect currentCustomRedirect)
        {
            // Get hold of the datastore
            using (DynamicDataStore store = DataStoreFactory.GetStore(typeof (CustomRedirect)))
            {
                //check if there is an exisiting object with matching property "OldUrl"
                CustomRedirect match =
                    store.Find<CustomRedirect>(OLD_URL_PROPERTY_NAME, currentCustomRedirect.OldUrl.ToLower())
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
        public virtual List<CustomRedirect> GetCustomRedirects(bool excludeIgnored)
        {
            // IEnumerable<CustomRedirect> customRedirects = null;
            using (DynamicDataStore store = DataStoreFactory.GetStore(typeof (CustomRedirect)))
            {
                IEnumerable<CustomRedirect> customRedirects;
                if (excludeIgnored)
                {
                    customRedirects = from s in store.Items<CustomRedirect>().OrderBy(cr => cr.OldUrl)
                        where s.State.Equals((int) GetState.Saved)
                        select s;
                }
                else
                {
                    customRedirects = from s in store.Items<CustomRedirect>().OrderBy(cr => cr.OldUrl)
                        select s;
                }
                return customRedirects != null ? customRedirects.ToList() : null;
            }
        }

        public virtual List<CustomRedirect> GetIgnoredRedirect()
        {
            using (DynamicDataStore store = DataStoreFactory.GetStore(typeof (CustomRedirect)))
            {
                IQueryable<CustomRedirect> customRedirects =
                    from s in store.Items<CustomRedirect>().OrderBy(cr => cr.OldUrl)
                    where s.State.Equals(GetState.Ignored)
                    select s;
                return customRedirects.ToList();
            }
        }

        public virtual void UnignoreRedirect()
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
            using (DynamicDataStore store = DataStoreFactory.GetStore(typeof (CustomRedirect)))
            {
                //find object with matching property "OldUrl"
                CustomRedirect match =
                    store.Find<CustomRedirect>(OLD_URL_PROPERTY_NAME, oldUrl.ToLower()).SingleOrDefault();
                if (match != null)
                    store.Delete(match);
            }
        }

        /// <summary>
        ///     Delete all CustomRedirect objects
        /// </summary>
        public virtual void DeleteAllCustomRedirects()
        {
            // In order to avoid a database timeout, we delete the items one by one.
            using (DynamicDataStore store = DataStoreFactory.GetStore(typeof (CustomRedirect)))
            {
                foreach (CustomRedirect redirect in GetCustomRedirects(false))
                {
                    store.Delete(redirect);
                }
            }
        }

        public virtual int DeleteAllIgnoredRedirects()
        {
            // In order to avoid a database timeout, we delete the items one by one.
            using (DynamicDataStore store = DataStoreFactory.GetStore(typeof (CustomRedirect)))
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
        public virtual List<CustomRedirect> SearchCustomRedirects(string searchWord)
        {
            using (DynamicDataStore store = DataStoreFactory.GetStore(typeof (CustomRedirect)))
            {
                IQueryable<CustomRedirect> customRedirects = from s in store.Items<CustomRedirect>()
                    where s.NewUrl.Contains(searchWord) || s.OldUrl.Contains(searchWord)
                    select s;

                return customRedirects != null ? customRedirects.ToList() : null;
            }
        }
    }
}