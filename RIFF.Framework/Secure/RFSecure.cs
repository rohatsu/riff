// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Interfaces.Encryption.AES;
using System;
using System.Security.Cryptography;
using System.Text;

namespace RIFF.Framework.Secure
{
    public static class RFSecure
    {
        public static string ComputeHash(string p)
        {
            return BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(p)));
        }

        public static RFSecureDecimal Create(decimal? value, byte[] encryptionKeyStream)
        {
            if (encryptionKeyStream != null)
            {
                var stringVal = value.HasValue ? value.Value.ToString() : "NULL";
                return new RFSecureDecimal
                {
                    CipherText = RIFF.Interfaces.Encryption.AES.AESUtils.SimpleEncrypt(stringVal, encryptionKeyStream, GenerateSalt(RFSecureDecimal.sSaltLength))
                };
            }
            throw new RFSystemException(typeof(RFSecure), "Empty encryption key.");
        }

        public static byte[] GenerateNewKey()
        {
            RFStatic.Log.Debug(typeof(RFKeyVault), "GenerateNewKey");
            return RIFF.Interfaces.Encryption.AES.AESUtils.NewKey();
        }

        public static byte[] GenerateSalt(int saltLength)
        {
            var salt = new byte[saltLength];
            AESUtils.Random.NextBytes(salt);
            return salt;
        }

        public static decimal? GetValue(this RFSecureDecimal secureDecimal, byte[] encryptionKey)
        {
            var str = RIFF.Interfaces.Encryption.AES.AESUtils.SimpleDecrypt(secureDecimal.CipherText, encryptionKey, RFSecureDecimal.sSaltLength);
            if (str == String.Empty || str == "NULL")
            {
                return null;
            }
            return Decimal.Parse(str);
        }
    }
}
