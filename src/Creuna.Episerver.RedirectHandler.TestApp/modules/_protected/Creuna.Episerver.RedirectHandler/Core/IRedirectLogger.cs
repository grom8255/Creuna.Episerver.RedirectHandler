using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Creuna.Episerver.RedirectHandler.Core
{
    public interface IRedirectLogger
    {
        void LogRedirect(string referrer, string oldUri, string newUri);
    }
}
