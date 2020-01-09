using HomeM8Service.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace HomeM8Service
{
    public static class Security
    {
        public static RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();

        public static Aes AES = new AesCryptoServiceProvider();

        #region Ctor

        static Security()
        {
            using (HomeM8Entities db = new HomeM8Entities())
            {
                var sr = new System.IO.StringReader(db.RSA.Take(1).Single().privateKey);

                var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

                var bufferRSAKey = (RSAParameters)xs.Deserialize(sr);

                RSA.ImportParameters(bufferRSAKey);
            }
            AES.IV = new byte[16];
        }

        #endregion

        #region New RSA Pair
        public static void GenerateRSAKeys()
        {
            RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider(2048);
            RSAParameters publicKey;
            RSAParameters privateKey;
            using (HomeM8Entities db = new HomeM8Entities())
            {
                publicKey = rsaProvider.ExportParameters(false);
                privateKey = rsaProvider.ExportParameters(true);
                string publicKeyXmlString;
                string privateKeyXmlString;

                #region Public key
                {
                    var sw = new System.IO.StringWriter();

                    var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

                    xs.Serialize(sw, publicKey);

                    publicKeyXmlString = sw.ToString();
                }
                #endregion

                #region Private Key
                {
                    var sw2 = new StringWriter();
                    var xs2 = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
                    xs2.Serialize(sw2, privateKey);
                    privateKeyXmlString = sw2.ToString();
                }
                #endregion

                db.Database.ExecuteSqlCommand("truncate table [RSA]");

                db.RSA.Add(new Models.RSA { privateKey = privateKeyXmlString.Trim(), publicKey = publicKeyXmlString.Trim() });

                db.SaveChanges();
            }
        } 
        #endregion

        #region Encrypt SHA256
        public static string EncryptSHA256(string plainText, string password)
        {
            if (plainText == null)
            {
                return null;
            }

            if (password == null)
            {
                password = String.Empty;
            }

            var bytesToBeEncrypted = Encoding.UTF8.GetBytes(plainText);
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            var bytesEncrypted = Encrypt(bytesToBeEncrypted, passwordBytes);

            return Convert.ToBase64String(bytesEncrypted);
        }
        private static byte[] Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;

            var saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);

                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }

                    encryptedBytes = ms.ToArray();
                }
            }
            return encryptedBytes;
        }
        #endregion

        #region Decrypt SHA256
        private static byte[] Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;

            var saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);

                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);
                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }

                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }

        public static string DecryptSHA256(string encryptedText, string password)
        {
            if (encryptedText == null)
            {
                return null;
            }

            if (password == null)
            {
                password = String.Empty;
            }

            var bytesToBeDecrypted = Convert.FromBase64String(encryptedText);
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            var bytesDecrypted = Decrypt(bytesToBeDecrypted, passwordBytes);

            return Encoding.UTF8.GetString(bytesDecrypted);
        }
        #endregion

        #region Encrypt RSA

        public static string EncryptRSA(string plainText)
        {
            var plainBytes = Encoding.Unicode.GetBytes(plainText);

            var cypherBytes = RSA.Encrypt(plainBytes, true);

            return Convert.ToBase64String(cypherBytes);
        }

        #endregion

        #region Decrypt RSA

        public static string DecryptRSA(string cypherText)
        {
            var cypherBytes = Convert.FromBase64String(cypherText);

            var plainBytes = RSA.Decrypt(cypherBytes, true);

            return Encoding.Unicode.GetString(plainBytes);
        }

        #endregion

        #region Encrypt SHA256 & RSA
        //public static string GetCypherBase64String(string plainText, string accessToken = null, string validationKey = null)
        //{
        //    if (validationKey != null) { }
        //    else if ((validationKey != null && accessToken != null) || (accessToken == null)) throw new InvalidOperationException();
        //    else
        //    {
        //        using (HomeM8Entities db = new HomeM8Entities())
        //        {
        //            validationKey = HomeM8.GetUserByAccessToken(accessToken, db)?.ValidationKey;
        //        }
        //    }

        //    var SHA256String = EncryptSHA256(plainText, validationKey);

        //    var plainBytes2 = Encoding.Unicode.GetBytes(SHA256String);

        //    var bytesCypher2 = RSA.Encrypt(plainBytes2, true);

        //    return Convert.ToBase64String(bytesCypher2);
        //}

        //public static string GetPlainString(string cypherBase64String, string accessToken = null, string validationKey = null)
        //{
        //    if (validationKey != null) { }
        //    else if (validationKey != null && accessToken != null) throw new InvalidOperationException();
        //    else
        //    {
        //        using (HomeM8Entities db = new HomeM8Entities())
        //        {
        //            //validationKey = HomeM8.GetUserByAccessToken(accessToken, db)?.ValidationKey;
        //        }
        //    }

        //    var cypherByteArray = Convert.FromBase64String(cypherBase64String);

        //    var bytesWithoutRSA = RSA.Decrypt(cypherByteArray, true);

        //    var stringWithSHA256 = Encoding.Unicode.GetString(bytesWithoutRSA);

        //    return DecryptSHA256(stringWithSHA256, validationKey);
        //}
        #endregion

        #region Encrypt AES
        public static string EncryptAES(byte[] Key, string secretMessage)
        {
            byte[] encryptedMessage;
            var derivedKey = Key;

            using (var cipherText = new MemoryStream())
            {
                using (var encryptor = AES.CreateEncryptor(Key, new byte[16]))
                {
                    using (var cryptoStream = new CryptoStream(cipherText, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] ciphertextMessage = Encoding.UTF8.GetBytes(secretMessage);
                        cryptoStream.Write(ciphertextMessage, 0, ciphertextMessage.Length);
                    }
                }

                encryptedMessage = cipherText.ToArray();
            }

            return Convert.ToBase64String(encryptedMessage);
        }
        #endregion

        #region Decrypt AES
        public static string DecryptAES(byte[] Key, string cypherText)
        {
            byte[] encryptedMessage = Convert.FromBase64String(cypherText);
            string decryptedMessage;
            var derivedKey = Key;

            using (var plainText = new MemoryStream())
            {
                using (var decryptor = AES.CreateDecryptor(Key,new byte[16]))
                {
                    using (var cryptoStream = new CryptoStream(plainText, decryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(encryptedMessage, 0, encryptedMessage.Length);
                    }
                }

                decryptedMessage = Encoding.UTF8.GetString(plainText.ToArray());
            }

            return decryptedMessage;
        }
        #endregion

        #region SignDataRSA
        /// <summary>
        /// Returns Base64String
        /// </summary>
        /// <param name="originalText"></param>
        /// <returns>Base64String</returns>
        public static string SignDataRSA(string originalText)
        {
            var encoder = new UTF8Encoding();
            var originalBytes = encoder.GetBytes(originalText);
            byte[] signedBytes;
            
            signedBytes = RSA.SignData(originalBytes, CryptoConfig.MapNameToOID("SHA512"));

            return Convert.ToBase64String(signedBytes);
        }

        #endregion
    }
}