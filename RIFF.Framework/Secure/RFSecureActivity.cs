// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;

namespace RIFF.Framework.Secure
{
    public class RFSecureActivity : RFActivity
    {
        private readonly RFKeyVaultKey _vaultKey;
        private RFKeyVault _vault;

        public RFSecureActivity(IRFProcessingContext context, string userName, RFKeyDomain keyDomain, RFEnum vaultEnum) : base(context, userName.ToLower().Trim())
        {
            _vaultKey = RFKeyVaultKey.Create(keyDomain, vaultEnum);
        }

        public RFKeyVault OpenKeyVault(string passwordHash)
        {
            _vault = Context.LoadDocumentContent<RFKeyVault>(_vaultKey);
            if (_vault == null)
            {
                _vault = new RFKeyVault();
            }
            var newUser = false;
            if (!_vault.HasUser(UserName))
            {
                _vault.CreateUser(UserName, passwordHash); // create if first login
                newUser = true;
                //throw new RFLogicException(this, "User {0} not created yet.", UserName);
            }
            if (!_vault.TryOpen(UserName, passwordHash))
            {
                throw new RFLogicException(this, "Invalid password for user {0}.", UserName);
            }
            if (newUser)
            {
                SaveKeyVault();
            }
            return _vault;
        }

        public void SaveKeyVault()
        {
            if (_vault != null && _vaultKey != null)
            {
                Context.SaveDocument(_vaultKey, _vault, false, null);
            }
        }
    }
}
