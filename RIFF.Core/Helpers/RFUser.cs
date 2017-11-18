// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;

namespace RIFF.Core
{
    public static class RFUser
    {
        public static object _cahceLock = new object();
        public static Dictionary<string, string> _userNameCache = new Dictionary<string, string>();

        public static string GetUserName(System.Security.Principal.IPrincipal principal)
        {
            lock (_cahceLock)
            {
                if (_userNameCache.ContainsKey(principal.Identity.Name))
                {
                    return _userNameCache[principal.Identity.Name];
                }
            }
            if (principal != null && principal.Identity != null && !string.IsNullOrWhiteSpace(principal.Identity.Name))
            {
                var localUser = IsLocalUser(principal);
                if (!string.IsNullOrWhiteSpace(localUser))
                {
                    return localUser;
                }
                else
                {
                    // try AD
                    try
                    {
                        var adRoot = RFSettings.GetAppSetting("ActiveDomainRoot", null);
                        var adUsername = RFSettings.GetAppSetting("ActiveDomainUsername", null);
                        var adPassword = RFSettings.GetAppSetting("ActiveDomainPassword", null);
                        if (!string.IsNullOrWhiteSpace(adRoot))
                        {
                            PrincipalContext context = null;
                            if (!string.IsNullOrWhiteSpace(adUsername))
                            {
                                context = new PrincipalContext(ContextType.Domain, null, adRoot, adUsername, adPassword);
                            }
                            else
                            {
                                context = new PrincipalContext(ContextType.Domain, null, adRoot);
                            }

                            var user = UserPrincipal.FindByIdentity(context, principal.Identity.Name);
                            if (user != null)
                            {
                                var displayName = String.Format("{0} {1}", user.GivenName, user.Surname);
                                lock (_cahceLock)
                                {
                                    if (!_userNameCache.ContainsKey(principal.Identity.Name))
                                    {
                                        _userNameCache[principal.Identity.Name] = displayName;
                                    }
                                }
                                return displayName;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        RFStatic.Log.Debug(typeof(RFUser), "Error retrieving username: {0}", ex.Message);
                    }
                }

                // fallback to NT username
                var name = principal.Identity.Name;
                if (name.Contains('\\'))
                {
                    name = name.Substring(name.IndexOf('\\') + 1);
                }
                // replace dots with spaces
                name = name.Replace('.', ' ');

                // remove underscore
                if (name.Contains('_'))
                {
                    name = name.Substring(0, name.IndexOf('_'));
                }

                return name.ToLower();
            }
            return "guest";
        }

        public static string IsLocalUser(System.Security.Principal.IPrincipal principal)
        {
            var domainContext = new PrincipalContext(ContextType.Machine);
            var matchedPrincipal = Principal.FindByIdentity(domainContext, principal.Identity.Name);
            if (matchedPrincipal != null)
            {
                return matchedPrincipal.DisplayName;
            }
            return null;
        }
    }
}
