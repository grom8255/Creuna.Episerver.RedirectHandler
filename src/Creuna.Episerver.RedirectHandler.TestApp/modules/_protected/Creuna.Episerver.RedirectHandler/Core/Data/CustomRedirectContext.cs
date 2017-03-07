using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Creuna.Episerver.RedirectHandler.Core.Data
{
    public class CustomRedirectContext : DbContext
    {
        public CustomRedirectContext() : base("EpiServerDb")
        {

        }

        public CustomRedirectContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {

        }

        public DbSet<CustomRedirect> CustomRedirects { get; set; }
    }
}
