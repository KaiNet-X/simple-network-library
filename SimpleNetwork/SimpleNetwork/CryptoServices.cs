using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SimpleNetwork
{
    internal static class CryptoServices
    {
        public static byte[] CreateHash(byte[] input)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(input);
        }

        public static byte[] CreateHash(string input)
        {
            return CreateHash(Encoding.UTF8.GetBytes(input));
        }

        public static byte[] KeyFromHash(byte[] hash)
        {
            byte[] key = new byte[128];

            for (int i = 0; i < 128; i++)
            {
                key[i] = hash[i % hash.Length];
            }
            return key;
        }

        public static void GenerateKeyPair(out RSAParameters PublicKey, out RSAParameters PrivateKey)
        {
            using (var prov = new RSACryptoServiceProvider(2048))
            {
                PrivateKey = prov.ExportParameters(true);
                PublicKey = prov.ExportParameters(false);
            }
        }

        public static byte[] EncryptRSA(byte[] bytes, RSAParameters PublicKey)
        {
            using (var provider = new RSACryptoServiceProvider())
            {
                provider.ImportParameters(PublicKey);
                return provider.Encrypt(bytes, false);
            }
        }

        public static byte[] DecryptRSA(byte[] bytes, RSAParameters PrivateKey)
        {
            using (var provider = new RSACryptoServiceProvider())
            {
                provider.ImportParameters(PrivateKey);
                return provider.Decrypt(bytes, false);
            }
        }

        public static byte[] EncryptAES(byte[] input, byte[] key)
        {
            byte[] result = null;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, GetCryptoAlgorithm().CreateEncryptor(key, new byte[16]), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(input, 0, input.Length);
                    cryptoStream.FlushFinalBlock();

                    result = memoryStream.ToArray();
                }
            }

            return result;
        }

        public static byte[] DecryptAES(byte[] input, byte[] key)
        {
            byte[] outputBytes = input;

            //string plaintext = string.Empty;

            using (MemoryStream memoryStream = new MemoryStream(outputBytes))
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, GetCryptoAlgorithm().CreateDecryptor(key, new byte[16]), CryptoStreamMode.Read))
                {
                    using (var outputStream = new MemoryStream())
                    {
                        cryptoStream.CopyTo(outputStream);
                        return outputStream.ToArray();
                    }
                }
            }

            //return Encoding.UTF8.GetBytes(plaintext);
        }

        private static RijndaelManaged GetCryptoAlgorithm()
        {
            RijndaelManaged algorithm = new RijndaelManaged();
            //set the mode, padding and block size
            algorithm.Padding = PaddingMode.PKCS7;
            algorithm.Mode = CipherMode.CBC;
            algorithm.KeySize = 128;
            algorithm.BlockSize = 128;
            return algorithm;
        }
    }
}
