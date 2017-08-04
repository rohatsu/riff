// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Concurrent;

namespace RIFF.Framework.Secure
{
    public static class RFLoginCache
    {
        // if this was not static we could have cached creds in one controller not the other, this
        // needs a better centralized solution (we could have different logons)
        private readonly static ConcurrentDictionary<string, string> _passwordHashCache = new ConcurrentDictionary<string, string>(); // md5 hashes

        public static string GetPasswordHash(string username)
        {
            string p;
            if (_passwordHashCache.TryGetValue(username, out p))
            {
                return p;
            }
            return null;
        }

        public static bool IsLoggedIn(string username)
        {
            return _passwordHashCache.ContainsKey(username);
        }

        public static string Login(string username, string passwordPlaintext)
        {
            var passwordHash = RFSecure.ComputeHash(passwordPlaintext);
            _passwordHashCache.AddOrUpdate(username, passwordHash, (k, v) => passwordHash);
            return GetPasswordHash(username);
        }

        public static bool Logout(string username)
        {
            try
            {
                string v;
                return _passwordHashCache.TryRemove(username, out v);
            }
            catch (Exception ex)
            {
                throw new RFLogicException(typeof(RFLoginCache), ex, "Logout");
            }
        }
    }
}
