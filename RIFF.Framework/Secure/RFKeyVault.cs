// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RIFF.Framework.Secure
{
    [DataContract]
    public class RFKeyVault : RFMappingDataSet<RFKeyVault.Key, RFKeyVault.Row>
    {
        [DataContract]
        public class Key : RFMappingKey
        {
            [DataMember]
            public string KeyID { get; set; }

            [DataMember]
            public string SecuredByKeyID { get; set; }

            [DataMember]
            public string SecuredByUsername { get; set; }

            public override object[] ComparisonFields()
            {
                return new string[] { KeyID, SecuredByKeyID, SecuredByUsername };
            }

            public override bool IsValid()
            {
                return !string.IsNullOrWhiteSpace(KeyID) && (!string.IsNullOrWhiteSpace(SecuredByKeyID) || !string.IsNullOrWhiteSpace(SecuredByUsername));
            }
        }

        [DataContract]
        public class Row : RFMappingDataRow<Key>
        {
            [DataMember]
            public byte[] CipherStream { get; set; }

            public override bool IsMissing()
            {
                return CipherStream == null;
            }
        }

        [IgnoreDataMember]
        public string AccessingPasswordHash { get; set; }

        [IgnoreDataMember]
        public string AccessingUsername { get; set; }

        public static string LOGIN_KEY_ID = "LoginKey";
        public static string MASTER_KEY_ID = "MasterKey";
        public static int MAX_RECURSION = 4;
        public static int SALT_LENGTH = 4;
        // runtime only

        public void ChangeMasterKey(byte[] newKey)
        {
            RFStatic.Log.Debug(this, "ChangeMasterKey under user {0}", AccessingUsername);

            var oldMasterKey = GetKey(MASTER_KEY_ID);
            if (oldMasterKey == null)
            {
                throw new RFSystemException(this, "Existing Master Key not accessible.");
            }

            // remove all entries with master key (other users')
            Rows.RemoveAll(r => r.Key.KeyID == MASTER_KEY_ID);
            BuildCache();

            // save new master key
            SecureKeyByPassword(MASTER_KEY_ID, newKey);

            // reencrypt all keys using the new master key
            foreach (var k in Rows.Where(r => r.Key.SecuredByKeyID == MASTER_KEY_ID))
            {
                var plainKey = RIFF.Interfaces.Encryption.AES.AESUtils.SimpleDecrypt(k.CipherStream, oldMasterKey, SALT_LENGTH);
                k.CipherStream = RIFF.Interfaces.Encryption.AES.AESUtils.SimpleEncrypt(plainKey, newKey, RFSecure.GenerateSalt(SALT_LENGTH));
            }
        }

        public void CreateUser(string username, string passwordHash)
        {
            RFStatic.Log.Debug(this, "CreateUser {0}", username);
            username = username.Trim().ToLower();
            if (HasUser(username))
            {
                throw new RFSystemException(this, "User {0} already present in key vault.");
            }

            var firstUserEver = !Rows.Any(r => r.Key.KeyID == LOGIN_KEY_ID);

            ResetLogin(username, passwordHash);
        }

        // runtime only
        public byte[] GetKey(string keyID, int level = 0)
        {
            // RFStatic.Log.Debug(this, "GetKey key {0} under user {1}", keyID, AccessingUsername);

            if (!IsOpen())
            {
                throw new RFSystemException(this, "Key Vault hasn't been opened yet.");
            }

            // prevent infinite recursion
            if (level == MAX_RECURSION)
            {
                return null;
            }

            // do we have it secured by our password?
            var userRow = Rows.FirstOrDefault(r => r.Key.KeyID == keyID && r.Key.SecuredByUsername == AccessingUsername);
            if (userRow != null)
            {
                return RIFF.Interfaces.Encryption.AES.AESUtils.SimpleDecryptWithPassword(userRow.CipherStream, AccessingPasswordHash, SALT_LENGTH);
            }

            // recursive try other keys we own
            foreach (var k in Rows.Where(r => r.Key.KeyID == keyID && r.Key.SecuredByKeyID != null))
            {
                var tryGetKey = GetKey(k.Key.SecuredByKeyID, level + 1);
                if (tryGetKey != null)
                {
                    return RIFF.Interfaces.Encryption.AES.AESUtils.SimpleDecrypt(k.CipherStream, tryGetKey, SALT_LENGTH);
                }
            }

            // key not accessible to us
            return null;
        }

        public bool HasUser(string username)
        {
            username = username.Trim().ToLower();
            return Rows.Any(r => r.Key.KeyID == LOGIN_KEY_ID && r.Key.SecuredByUsername == username);
        }

        public void ResetUser(string username, string passwordHash) // reset ourselves OK
        {
            RFStatic.Log.Debug(this, "ResetUser {0}", username);

            username = username.Trim().ToLower();

            // reset held keys
            var ownedKeys = new Dictionary<string, byte[]>(); // cache owned keys
            foreach (var r in Rows.Where(r => r.Key.SecuredByUsername == username && r.Key.KeyID != LOGIN_KEY_ID))
            {
                if (!ownedKeys.ContainsKey(r.Key.KeyID))
                {
                    var ownedKey = GetKey(r.Key.KeyID); // we need to be able to access this key as ourselves
                    if (ownedKey != null)
                    {
                        ownedKeys.Add(r.Key.KeyID, ownedKey); // cache
                    }
                    else
                    {
                        throw new RFSystemException(this, "Unable to reset user {0} as key {1} not accessible to user {2}", username, r.Key.KeyID, AccessingUsername);
                    }
                }
            }

            // reset login
            ResetLogin(username, passwordHash);

            // iterate again this time recrypting keys
            foreach (var r in Rows.Where(r => r.Key.SecuredByUsername == username && r.Key.KeyID != LOGIN_KEY_ID))
            {
                var ownedKey = ownedKeys[r.Key.KeyID]; // stored plain key
                r.CipherStream = RIFF.Interfaces.Encryption.AES.AESUtils.SimpleEncryptWithPassword(ownedKey, passwordHash, RFSecure.GenerateSalt(SALT_LENGTH)); // recrypt using new password
            }
        }

        public void SecureKeyByAnotherKey(string keyID, byte[] keyStream, string secureByKeyID)
        {
            RFStatic.Log.Debug(this, "SecureKeyByAnotherKey key {0} using key {1} under user {2}", keyID, secureByKeyID, AccessingUsername);

            if (!IsOpen())
            {
                throw new RFSystemException(this, "Key Vault hasn't been opened yet.");
            }

            if (keyStream == null || string.IsNullOrWhiteSpace(keyID))
            {
                throw new RFSystemException(this, "Empty key provided.");
            }

            var secureByKey = GetKey(secureByKeyID);
            if (secureByKey != null)
            {
                var securedKey = GetOrCreateMapping(new Key
                {
                    KeyID = keyID,
                    SecuredByKeyID = secureByKeyID,
                    SecuredByUsername = null
                });
                securedKey.CipherStream = RIFF.Interfaces.Encryption.AES.AESUtils.SimpleEncrypt(keyStream, secureByKey, RFSecure.GenerateSalt(SALT_LENGTH));
            }
            else
            {
                throw new RFSystemException(this, "Requested encryption key {0} not accessible.", secureByKeyID);
            }
        }

        public void SecureKeyByPassword(string keyID, byte[] keyStream)
        {
            SecureKeyForAnotherUser(keyID, keyStream, AccessingUsername, AccessingPasswordHash);
        }

        public void SecureKeyForAnotherUser(string keyID, byte[] keyStream, string username, string passwordHash)
        {
            RFStatic.Log.Debug(this, "SecureKeyForAnotherUser key {0} by user {1} for user {2}", keyID, AccessingUsername, username);

            if (!IsOpen())
            {
                throw new RFSystemException(this, "Key Vault hasn't been opened yet.");
            }
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(passwordHash))
            {
                throw new RFSystemException(this, "Empty credentials provided.");
            }
            username = username.ToLower().Trim();
            if (keyStream == null || string.IsNullOrWhiteSpace(keyID))
            {
                throw new RFSystemException(this, "Empty key provided.");
            }
            var securedKey = GetOrCreateMapping(new Key
            {
                KeyID = keyID,
                SecuredByKeyID = null,
                SecuredByUsername = username
            });
            securedKey.CipherStream = RIFF.Interfaces.Encryption.AES.AESUtils.SimpleEncryptWithPassword(keyStream, passwordHash, RFSecure.GenerateSalt(SALT_LENGTH));
        }

        public bool TryOpen(string username, string passwordHash)
        {
            RFStatic.Log.Debug(this, "TryOpen {0}", username);
            var loginMapping = GetMapping(GetLoginKey(username));
            if (loginMapping != null)
            {
                var check = RIFF.Interfaces.Encryption.AES.AESUtils.SimpleDecryptWithPassword(loginMapping.CipherStream, passwordHash, SALT_LENGTH);
                if (check != null && check.Length == 1)
                {
                    AccessingUsername = username.ToLower().Trim();
                    AccessingPasswordHash = passwordHash;
                    return true;
                }
            }
            return false; // user not found
        }

        private static Key GetLoginKey(string username)
        {
            return new Key
            {
                KeyID = LOGIN_KEY_ID,
                SecuredByUsername = username.ToLower().Trim(),
                SecuredByKeyID = null
            };
        }

        private bool IsOpen()
        {
            return !string.IsNullOrWhiteSpace(AccessingUsername) && !string.IsNullOrWhiteSpace(AccessingPasswordHash);
        }

        private void ResetLogin(string username, string passwordHash)
        {
            RFStatic.Log.Debug(this, "ResetLogin {0}", username);

            username = username.Trim().ToLower();
            var loginKey = GetLoginKey(username);
            var loginMapping = GetOrCreateMapping(loginKey);
            loginMapping.CipherStream = RIFF.Interfaces.Encryption.AES.AESUtils.SimpleEncryptWithPassword(new byte[] { 0x66 }, passwordHash, RFSecure.GenerateSalt(SALT_LENGTH));
        }
    }

    [DataContract]
    public class RFKeyVaultKey : RFCatalogKey
    {
        [DataMember]
        public RFEnum Enum { get; set; }

        public static RFKeyVaultKey Create(RFKeyDomain domain, RFEnum _enum)
        {
            return domain.Associate(new RFKeyVaultKey
            {
                Plane = RFPlane.User,
                Enum = _enum,
                StoreType = RFStoreType.Document,
                GraphInstance = null
            }) as RFKeyVaultKey;
        }

        public override string FriendlyString()
        {
            return Enum.ToString();
        }
    }
}
