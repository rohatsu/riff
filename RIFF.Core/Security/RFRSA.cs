// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace RIFF.Core
{
    public static class RFPublicRSA
    {
        public static KeyValuePair<string, DateTime> GetHost(string token1, string token2)
        {
            var publicKey = RFInternaRSA.GetPublicKey();
            if (RFInternaRSA.VerifyData(token1, token2, publicKey.ExportParameters(false)))
            {
                var decodedLicense = Encoding.ASCII.GetString(Convert.FromBase64String(token1));

                var tokens = decodedLicense.Split('|');
                var expiry = DateTime.ParseExact(tokens[1], "yyyy-MM-dd", null);
                if (expiry > DateTime.Now)
                {
                    return new KeyValuePair<string, DateTime>(tokens[0], expiry);
                }
                return new KeyValuePair<string, DateTime>("Evaluation license", DateTime.Today);
            }
            return new KeyValuePair<string, DateTime>("Evaluation license", DateTime.Today);
        }

        public static string SignData(string message, RSAParameters privateKey)
        {
            byte[] signedBytes;
            using (var rsa = new RSACryptoServiceProvider())
            {
                var encoder = new UTF8Encoding();
                var originalData = encoder.GetBytes(message);

                try
                {
                    rsa.ImportParameters(privateKey);
                    signedBytes = rsa.SignData(originalData, CryptoConfig.MapNameToOID("SHA512"));
                }
                catch (CryptographicException e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
            return Convert.ToBase64String(signedBytes);
        }
    }

    internal static class RFInternaRSA
    {
        internal static RSACryptoServiceProvider GetPublicKey()
        {
            // WARNING: any changes to the verification key or verification code are strictly prohibited
            var publicKeyData = new byte[] { 6, 2, 0, 0, 0, 36, 0, 0, 82, 83, 65, 49, 0, 4, 0, 0, 1, 0, 1, 0,
                77, 182, 71, 133, 1, 224, 18, 142, 95, 166, 155, 146, 77, 194, 229, 124, 6, 37, 234, 103,
                237, 253, 157, 238, 40, 251, 221, 17, 159, 73, 51, 120, 66, 36, 23, 89, 211, 227, 11, 12,
                220, 168, 144, 171, 82, 160, 218, 66, 47, 140, 2, 166, 231, 130, 189, 202, 194, 90, 150,
                28, 240, 234, 24, 148, 15, 171, 25, 141, 132, 128, 169, 29, 191, 56, 33, 237, 43, 219,
                1, 69, 87, 47, 201, 105, 190, 224, 160, 186, 188, 9, 182, 112, 219, 169, 20, 80, 176,
                124, 46, 90, 168, 108, 94, 88, 37, 91, 163, 88, 9, 178, 200, 183, 184, 104, 54, 78,
                45, 136, 37, 166, 203, 90, 202, 62, 247, 193, 178, 219 };

            var publicKey = new RSACryptoServiceProvider();
            publicKey.ImportCspBlob(publicKeyData);
            return publicKey;
        }

        internal static RSACryptoServiceProvider GetPublicKeyFromAssembly(Assembly assembly)
        {
            var pk = new byte[] { 0x1f, 0xaa };

            var rawPublicKeyData = assembly.GetName().GetPublicKey();

            int extraHeadersLen = 12;
            int bytesToRead = rawPublicKeyData.Length - extraHeadersLen;
            byte[] publicKeyData = new byte[bytesToRead];
            Buffer.BlockCopy(rawPublicKeyData, extraHeadersLen, publicKeyData, 0, bytesToRead);

            var publicKey = new RSACryptoServiceProvider();
            publicKey.ImportCspBlob(publicKeyData);

            return publicKey;
        }

        internal static bool VerifyData(string originalMessage, string signedMessage, RSAParameters publicKey)
        {
            bool success = false;
            using (var rsa = new RSACryptoServiceProvider())
            {
                var encoder = new UTF8Encoding();
                var bytesToVerify = encoder.GetBytes(originalMessage);
                var signedBytes = Convert.FromBase64String(signedMessage);
                try
                {
                    rsa.ImportParameters(publicKey);

                    var Hash = new SHA512Managed();

                    var hashedData = Hash.ComputeHash(signedBytes);

                    success = rsa.VerifyData(bytesToVerify, CryptoConfig.MapNameToOID("SHA512"), signedBytes);
                }
                catch (CryptographicException e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
            return success;
        }
    }
}
