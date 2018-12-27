// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Framework.Secure;
using RIFF.Web.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace RIFF.Web.Core.Controllers
{
    public class RoleController : RIFFController
    {
        public RoleController(IRFProcessingContext context, RFEngineDefinition engineConfig) : base(context, engineConfig)
        {
        }

        [HttpPost]
        [RFControllerAuthorize(AccessLevel = RFAccessLevel.Write, ResponseType = ResponseType.Json)]
        public JsonResult AddMember(FormCollection collection)
        {
            try
            {
                var rolename = collection["RoleName"];
                var username = collection["Username"];

                if (!string.IsNullOrWhiteSpace(rolename) && !string.IsNullOrWhiteSpace(username))
                {
                    Context.UserLog.LogEntry(new RFUserLogEntry
                    {
                        Action = "AddMember",
                        Area = "Role",
                        IsUserAction = true,
                        IsWarning = false,
                        Username = Username,
                        Description = String.Format("Added user {0} to role {1}", username, rolename)
                    });
                    return Json(Context.UserRole.AddMember(rolename, username));
                }
            }
            catch (Exception ex)
            {
                return Json(JsonError.Throw("AddMember", ex));
            }
            return Json(JsonError.Throw("AddMember", "Internal system error."));
        }

        [HttpPost]
        public ActionResult BackupMasterKey(string p)
        {
            try
            {
                var keys = new List<object>();
                RFLoginCache.Login(LoginUsername, p);
                // find all vaults
                var vaults = Context.GetKeysByType<RFKeyVaultKey>();
                var sb = new StringBuilder();
                sb.AppendLine("VaultName,KeyID,Base64");
                if (vaults.Any())
                {
                    foreach (var vaultKey in vaults.Values)
                    {
                        using (var secure = new RFSecureActivity(Context, LoginUsername, new RFSimpleKeyDomain(vaultKey.Root), vaultKey.Enum))
                        {
                            var vault = secure.OpenKeyVault(RFLoginCache.GetPasswordHash(LoginUsername));
                            var masterKey = vault.GetKey(RFKeyVault.MASTER_KEY_ID);
                            if (masterKey != null)
                            {
                                sb.AppendFormat("{0},{1},{2}{3}", vaultKey.Enum.ToString(), RFKeyVault.MASTER_KEY_ID, Convert.ToBase64String(masterKey), Environment.NewLine);
                            }
                        }
                    }
                }

                Context.UserLog.LogEntry(new RFUserLogEntry
                {
                    Action = "BackupMasterKey",
                    Area = "Encryption",
                    Description = "Backed up Master Key.",
                    IsUserAction = true,
                    IsWarning = false,
                    Username = Username,
                    Timestamp = DateTimeOffset.Now
                });

                return File(System.Text.Encoding.ASCII.GetBytes(sb.ToString()), "text/csv", string.Format("Master Key Backup {0}.csv", RFDate.Today().ToString("yyyy-MM-dd")));
            }
            catch (Exception ex)
            {
                return Content("<html><body><h3>Error</h3><p>" + ex.Message + "</p></body></html>");
            }
        }

        [HttpPost]
        public JsonResult ChangePassword(string old_p, string new_p)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(old_p) && !string.IsNullOrWhiteSpace(new_p))
                {
                    RFLoginCache.Login(LoginUsername, old_p);
                    var oldPasswordHash = RFLoginCache.GetPasswordHash(LoginUsername);
                    RFLoginCache.Login(LoginUsername, new_p);
                    var newPasswordHash = RFLoginCache.GetPasswordHash(LoginUsername);

                    var vaults = Context.GetKeysByType<RFKeyVaultKey>();
                    if (vaults.Any())
                    {
                        foreach (var vaultKey in vaults.Values)
                        {
                            using (var secure = new RFSecureActivity(Context, LoginUsername, new RFSimpleKeyDomain(vaultKey.Root), vaultKey.Enum))
                            {
                                var vault = secure.OpenKeyVault(oldPasswordHash);
                                vault.ResetUser(LoginUsername, newPasswordHash);
                                secure.SaveKeyVault();
                            }
                        }
                    }
                }

                Context.UserLog.LogEntry(new RFUserLogEntry
                {
                    Action = "ChangePassword",
                    Area = "Encryption",
                    Description = "Changed password.",
                    IsUserAction = true,
                    IsWarning = false,
                    Username = Username,
                    Timestamp = DateTimeOffset.Now
                });

                return Json(true);
            }
            catch (Exception ex)
            {
                RFLoginCache.Logout(LoginUsername);
                return Json(JsonError.Throw("ChangePassword", ex));
            }
        }

        [HttpPost]
        public JsonResult GetKeys(string p)
        {
            try
            {
                var keys = new List<object>();
                RFLoginCache.Login(LoginUsername, p);
                // find all vaults
                var vaults = Context.GetKeysByType<RFKeyVaultKey>();
                if (vaults.Any())
                {
                    foreach (var vaultKey in vaults.Values)
                    {
                        using (var secure = new RFSecureActivity(Context, LoginUsername, new RFSimpleKeyDomain(vaultKey.Root), vaultKey.Enum))
                        {
                            var vault = secure.OpenKeyVault(RFLoginCache.GetPasswordHash(LoginUsername));
                            foreach (var key in vault.Rows)
                            {
                                keys.Add(new
                                {
                                    Vault = vaultKey.Enum.ToString(),
                                    KeyID = key.Key.KeyID,
                                    SecuredByKeyID = key.Key.SecuredByKeyID,
                                    SecuredByUsername = key.Key.SecuredByUsername,
                                    CipherStream = key.CipherStream,
                                    Accessible = vault.GetKey(key.Key.KeyID) != null && (string.IsNullOrWhiteSpace(key.Key.SecuredByUsername) || key.Key.SecuredByUsername.Equals(LoginUsername, StringComparison.InvariantCultureIgnoreCase))
                                });
                            }
                        }
                    }
                }
                return Json(new
                {
                    Keys = keys
                });
            }
            catch (Exception ex)
            {
                return Json(JsonError.Throw("GetKeys", ex));
            }
        }

        public JsonResult GetPermissions()
        {
            var permissions = Context.UserRole.GetPermissions(null, null);

            return Json(new
            {
                Permissions = permissions.Where(m => !string.IsNullOrWhiteSpace(m.UserName)).Select(p => new
                {
                    p.Area,
                    p.Controller,
                    p.Permission,
                    p.RoleName,
                    p.UserName,
                    p.IsAllowed
                })
            });
        }

        public JsonResult GetRoles()
        {
            var permissions = Context.UserRole.GetPermissions(null, null);

            return Json(new
            {
                Roles = permissions.GroupBy(p => p.RoleName).Select(p => new
                {
                    RoleName = p.Key,
                    NumMembers = p.Where(m => !string.IsNullOrWhiteSpace(m.UserName)).Select(m => m.UserName).Distinct().Count(),
                    NumPermissions = p.GroupBy(m => m.UserName).First().Count(),
                    Members = p.Where(m => !string.IsNullOrWhiteSpace(m.UserName)).Select(m => m.UserName).Distinct().Select(u => new
                    {
                        RoleName = p.Key,
                        Username = u
                    }),
                    Permissions = p.GroupBy(m => m.UserName).First().Select(m => new
                    {
                        m.Area,
                        m.Controller,
                        m.Permission,
                        m.IsAllowed
                    })
                })
            });
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult RemoveMember(FormCollection collection)
        {
            try
            {
                var rolename = collection["RoleName"];
                var username = collection["Username"];

                if (!string.IsNullOrWhiteSpace(rolename) && !string.IsNullOrWhiteSpace(username))
                {
                    Context.UserLog.LogEntry(new RFUserLogEntry
                    {
                        Action = "RemoveMember",
                        Area = "Role",
                        IsUserAction = true,
                        IsWarning = false,
                        Username = Username,
                        Description = String.Format("Removed user {0} from role {1}", username, rolename)
                    });
                    return Json(Context.UserRole.RemoveMember(rolename, username));
                }
            }
            catch (Exception ex)
            {
                return Json(JsonError.Throw("RemoveMember", ex));
            }
            return Json(JsonError.Throw("RemoveMember", "Internal system error."));
        }

        [HttpPost]
        public JsonResult ResetMaster(string p)
        {
            try
            {
                var passwordHash = RFLoginCache.Login(LoginUsername, p);
                var vaults = Context.GetKeysByType<RFKeyVaultKey>();
                if (vaults.Any())
                {
                    foreach (var vaultKey in vaults.Values)
                    {
                        using (var secure = new RFSecureActivity(Context, LoginUsername, new RFSimpleKeyDomain(vaultKey.Root), vaultKey.Enum))
                        {
                            var vault = secure.OpenKeyVault(passwordHash);
                            var newKey = RFSecure.GenerateNewKey();
                            vault.ChangeMasterKey(newKey);
                            secure.SaveKeyVault();
                        }
                    }
                }

                Context.UserLog.LogEntry(new RFUserLogEntry
                {
                    Action = "ResetMaster",
                    Area = "Encryption",
                    Description = "Reset Master Key.",
                    IsUserAction = true,
                    IsWarning = false,
                    Username = Username,
                    Timestamp = DateTimeOffset.Now
                });

                return Json(true);
            }
            catch (Exception ex)
            {
                RFLoginCache.Logout(LoginUsername);
                return Json(JsonError.Throw("ResetMaster", ex));
            }
        }
    }
}
