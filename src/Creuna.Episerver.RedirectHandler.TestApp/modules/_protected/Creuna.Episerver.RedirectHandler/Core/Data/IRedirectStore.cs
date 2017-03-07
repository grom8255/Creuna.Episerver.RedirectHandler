using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;
using EPiServer.Data;

namespace Creuna.Episerver.RedirectHandler.Core.Data
{
    public interface IRedirectStore
    {
        void Delete(CustomRedirect match);
        CustomRedirect FindByOldUrl(string oldUrl);
        void Replace(CustomRedirect currentCustomRedirect, Guid oldId);
        void Insert(CustomRedirect currentCustomRedirect);
    }
}
