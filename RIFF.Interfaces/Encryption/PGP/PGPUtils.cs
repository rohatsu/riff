// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.IO;
using System.Linq;

namespace RIFF.Interfaces.Encryption.PGP
{
    public static class PGPUtils
    {
        public static Stream Decrypt(Stream input, String privateKeyPath, String privateKeyPass)
        {
            input = PgpUtilities.GetDecoderStream(input);
            try
            {
                var pgpObjF = new PgpObjectFactory(input);
                PgpEncryptedDataList enc;
                var obj = pgpObjF.NextPgpObject();
                if (obj is PgpEncryptedDataList)
                {
                    enc = (PgpEncryptedDataList)obj;
                }
                else
                {
                    enc = (PgpEncryptedDataList)pgpObjF.NextPgpObject();
                }

                foreach (PgpPublicKeyEncryptedData pbe in enc.GetEncryptedDataObjects().Cast<PgpPublicKeyEncryptedData>())
                {
                    var privKey = GetPrivateKey(privateKeyPath, pbe.KeyId, privateKeyPass);
                    if (privKey == null)
                    {
                        continue;
                    }
                    Stream clear;
                    clear = pbe.GetDataStream(privKey);
                    var plainFact = new PgpObjectFactory(clear);
                    var message = plainFact.NextPgpObject();
                    if (message is PgpCompressedData)
                    {
                        var cData = (PgpCompressedData)message;
                        var compDataIn = cData.GetDataStream();
                        var o = new PgpObjectFactory(compDataIn);
                        message = o.NextPgpObject();
                        if (message is PgpOnePassSignatureList)
                        {
                            message = o.NextPgpObject();
                            PgpLiteralData Ld = null;
                            Ld = (PgpLiteralData)message;
                            return Ld.GetInputStream();
                        }
                        else
                        {
                            PgpLiteralData Ld = null;
                            Ld = (PgpLiteralData)message;
                            return Ld.GetInputStream();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            return null;
        }

        public static PgpPrivateKey GetPrivateKey(string privateKeyPath, long secretKeyID, string privateKeyPass)
        {
            using (Stream keyIn = File.OpenRead(privateKeyPath))
            using (Stream inputStream = PgpUtilities.GetDecoderStream(keyIn))
            {
                var decoderStream = PgpUtilities.GetDecoderStream(keyIn);

                var factory = new PgpObjectFactory(decoderStream);
                PgpObject o = null;
                do
                {
                    o = factory.NextPgpObject();
                    if (o is PgpSecretKeyRing)
                    {
                        var pgpSecKey = ((PgpSecretKeyRing)o).GetSecretKey(secretKeyID);
                        if (pgpSecKey != null)
                        {
                            return pgpSecKey.ExtractPrivateKey(string.IsNullOrWhiteSpace(privateKeyPass) ? null : privateKeyPass.ToCharArray());
                        }
                    }
                } while (o != null);
            }
            return null;
        }
    }
}
