// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RIFF.Core
{
    public class RFUserPermission
    {
        public string Area { get; set; }

        public string Controller { get; set; }

        public bool IsAllowed { get; set; }

        public string Permission { get; set; }

        public string RoleName { get; set; }

        public string UserName { get; set; }

        public int UserRoleID { get; set; }
    }

    internal class RFSQLUserRole : IRFUserRole
    {
        protected string _connectionString;

        public RFSQLUserRole(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool AddMember(string rolename, string username)
        {
            try
            {
                var permissions = GetPermissions(null, null);
                var roleID = permissions.FirstOrDefault(p => p.RoleName == rolename);
                if (roleID != null)
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        var insertStatement =
                            "INSERT INTO [RIFF].[UserRoleMembership] ( [UserRoleID], [UserName] ) values ( @UserRoleID, @UserName )";
                        using (var insertCommand = new SqlCommand(insertStatement, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@UserName", username);
                            insertCommand.Parameters.AddWithValue("@UserRoleID", roleID.UserRoleID);
                            return (insertCommand.ExecuteNonQuery() == 1);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        public List<RFUserPermission> GetPermissions(string username, string permission)
        {
            var permissions = new List<RFUserPermission>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var getQuery =
                        "SELECT [RoleName], [UserName], [Area], [Controller], [Permission], [IsAllowed], [UserRoleID] FROM [RIFF].[UserPermissionView]";
                    if (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(permission))
                    {
                        username = NormalizeUsername(username);
                        var criteria = new List<string>();
                        if (!string.IsNullOrWhiteSpace(username))
                        {
                            criteria.Add("[UserName] = @UserName");
                        }
                        if (!string.IsNullOrWhiteSpace(permission))
                        {
                            criteria.Add("[Permission] = @Permission");
                        }
                        getQuery += " WHERE " + string.Join(" AND ", criteria);
                    }
                    using (var getCommand = new SqlCommand(getQuery, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(username))
                        {
                            getCommand.Parameters.AddWithValue("@Username", username);
                        }
                        if (!string.IsNullOrWhiteSpace(permission))
                        {
                            getCommand.Parameters.AddWithValue("@Permission", permission);
                        }
                        using (var reader = getCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            if (dataTable.Rows.Count > 0)
                            {
                                permissions.AddRange(from DataRow row in dataTable.Rows
                                                     select new RFUserPermission
                                                     {
                                                         Permission = row["Permission"].ToString(),
                                                         Area = row["Area"].ToString(),
                                                         Controller = row["Controller"].ToString(),
                                                         IsAllowed = (bool)row["IsAllowed"],
                                                         RoleName = row["RoleName"].ToString(),
                                                         UserName = row["UserName"].ToString(),
                                                         UserRoleID = (int)row["UserRoleID"]
                                                     });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Exception(this, ex, "Unable to read permissions");
            }

            return permissions;
        }

        public bool HasPermission(string username, string area, string controller, string permission)
        {
            if (!RequiresPermission(username))
            {
                return true;
            }
            // strip domain info
            username = NormalizeUsername(username);
            controller = NormalizeController(controller);

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(permission) || string.IsNullOrWhiteSpace(area))
            {
                RFStatic.Log.Warning(this, "Invalid request for HasPermission");
                return false;
            }
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    var permissionQuery = "SELECT [IsAllowed] FROM [RIFF].[UserPermissionView] WHERE LOWER([Area]) = @Area AND LOWER([UserName]) = @UserName AND LOWER([Permission]) = @Permission AND LOWER([Controller]) = @Controller";
                    using (var permissionCommand = new SqlCommand(permissionQuery, connection))
                    {
                        permissionCommand.Parameters.AddWithValue("@Area", area.Trim().ToLower());
                        permissionCommand.Parameters.AddWithValue("@UserName", username.Trim().ToLower());
                        permissionCommand.Parameters.AddWithValue("@Controller", controller.Trim().ToLower());
                        permissionCommand.Parameters.AddWithValue("@Permission", permission.Trim().ToLower());
                        using (var reader = permissionCommand.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                        {
                            var dataTable = new DataTable();
                            dataTable.Load(reader);
                            if (dataTable.Rows.Count > 0)
                            {
                                return (bool)dataTable.Rows[0]["IsAllowed"];
                            }
                            else
                            {
                                return false; // no entry for this user
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Exception(this, ex, "Unable to read permissions for user {0} in area {1} and permission {2}", username, area, permission);
            }
            return false;
        }

        public bool RemoveMember(string rolename, string username)
        {
            try
            {
                var permissions = GetPermissions(null, null);
                var roleID = permissions.FirstOrDefault(p => p.RoleName == rolename && p.UserName == username);
                if (roleID != null)
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        var deleteStatement =
                            "DELETE FROM [RIFF].[UserRoleMembership] WHERE [UserRoleID] = @UserRoleID AND [UserName] = @UserName";
                        using (var deleteCommand = new SqlCommand(deleteStatement, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@UserName", username);
                            deleteCommand.Parameters.AddWithValue("@UserRoleID", roleID.UserRoleID);
                            return (deleteCommand.ExecuteNonQuery() == 1);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        public bool RequiresPermission(string username)
        {
            // check for global flag
            if (RFSettings.GetAppSetting("DisablePermissions", false))
            {
                return false;
            }
            // or global admin
            if (GetPermissions(username, "All")
                    .Any(p => p.Controller == "All" && p.Area == "All" && p.Permission == "All" && p.IsAllowed))
            {
                return false;
            }
            return true;
        }

        protected static string NormalizeController(string controller)
        {
            if (controller.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
            {
                controller = controller.Substring(0, controller.LastIndexOf("Controller", StringComparison.OrdinalIgnoreCase));
            }
            return controller;
        }

        protected static string NormalizeUsername(string username)
        {
            if (username.Contains('\\'))
            {
                username = username.Substring(username.IndexOf('\\') + 1);
            }
            return username;
        }
    }

    public interface IRFUserRole
    {
        bool AddMember(string rolename, string username);

        List<RFUserPermission> GetPermissions(string username, string permission);

        bool HasPermission(string username, string area, string controller, string permission);

        bool RemoveMember(string rolename, string username);

        bool RequiresPermission(string username);
    }
}
