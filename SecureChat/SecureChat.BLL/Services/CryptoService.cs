using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SecureChat.BLL.Interfaces;
using SecureChat.BLL.Settings;

namespace SecureChat.BLL.Services
{
    public class CryptoService : ICryptoService
    {
        private readonly CryptoSettings _settings;

        public CryptoService(IOptions<CryptoSettings> settings)
        {
            _settings = settings.Value;
        }

        public string GetServerPublicKey()
        {
            return _settings.ServerPublicKey;
        }

        public string GenerateAesKeyBase64()
        {
            using var aes = Aes.Create();
            aes.GenerateKey(); // AES-256
            return Convert.ToBase64String(aes.Key);
        }

        public string DecryptAesKeyFromClient(string encryptedAesKeyBase64)
        {
            byte[] dataToDecrypt = Convert.FromBase64String(encryptedAesKeyBase64);
            using var rsa = RSA.Create();

            byte[] privateKeyBytes = Convert.FromBase64String(_settings.ServerPrivateKey);
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

            byte[] decryptedData = rsa.Decrypt(dataToDecrypt, RSAEncryptionPadding.Pkcs1);

            return Encoding.UTF8.GetString(decryptedData);
        }

        public string EncryptAesKeyForClient(string rawAesKeyBase64, string clientPublicKeyBase64)
        {
            byte[] dataToEncrypt = Encoding.UTF8.GetBytes(rawAesKeyBase64);
            using var rsa = RSA.Create();

            byte[] publicKeyBytes = Convert.FromBase64String(clientPublicKeyBase64);
            rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            byte[] encryptedData = rsa.Encrypt(dataToEncrypt, RSAEncryptionPadding.Pkcs1);
            return Convert.ToBase64String(encryptedData);
        }

        public string EncryptMessage(string plainText, string aesKeyBase64)
        {
            byte[] key = Convert.FromBase64String(aesKeyBase64);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV(); // generating new IV for each message (16 bytes)

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            // writing the IV at the begining of the stream so we can read it when decrypting it
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public string DecryptMessage(string cipherTextBase64, string aesKeyBase64)
        {
            byte[] fullCipher = Convert.FromBase64String(cipherTextBase64);
            byte[] key = Convert.FromBase64String(aesKeyBase64);

            using var aes = Aes.Create();
            aes.Key = key;

            // extracting IV from first 16 bytes
            byte[] iv = new byte[aes.BlockSize / 8]; // block size = 128-bit always
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }


        public string EncryptForDatabase(string plainText)
        {
            return EncryptMessage(plainText, _settings.DbMasterKey);
        }

        public string DecryptFromDatabase(string cipherTextBase64)
        {
            return DecryptMessage(cipherTextBase64, _settings.DbMasterKey);
        }
    }
}