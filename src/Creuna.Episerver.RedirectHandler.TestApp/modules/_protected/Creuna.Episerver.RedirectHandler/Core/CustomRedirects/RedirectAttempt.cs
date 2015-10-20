using System;

namespace Creuna.Episerver.RedirectHandler.Core.CustomRedirects
{
    public struct RedirectAttempt
    {
        private readonly bool _redirected;
        private readonly string _newUrl;

        private RedirectAttempt(bool redirected, string newUrl)
        {
            _redirected = redirected;
            _newUrl = newUrl;
        }

        public static RedirectAttempt Miss()
        {
            return new RedirectAttempt(false, null);
        }

        public static RedirectAttempt Success(string newUrl)
        {
            if (string.IsNullOrWhiteSpace(newUrl))
                throw new ArgumentException("NewUrl cannot be null or empty.", "newUrl");
            return new RedirectAttempt(true, newUrl);
        }

        public bool Redirected { get { return _redirected; } }
        public string NewUrl { get { return _newUrl; } }

        public override int GetHashCode()
        {
            var result = 17;
            result = 31 * result + (Redirected ? 1 : 0);
            result = 31 * result + NewUrl.GetHashCode();
            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() != this.GetType())
                return false;
            var other = (RedirectAttempt)obj;
            return other.Redirected == Redirected
                   && other.NewUrl == NewUrl;
        }
    }
}
