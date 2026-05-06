namespace SecureChat.BLL.Interfaces
{
    public interface ICryptoService
    {
        // 1. RSA operations (for exchanging session keys)
        string DecryptAesKeyFromClient(string encryptedAesKeyBase64);
        string EncryptAesKeyForClient(string rawAesKeyBase64, string clientPublicKeyXml);
        string GetServerPublicKey();

        // 2. AES operations (for encrypting and decrypting chats during session)
        string GenerateAesKeyBase64();
        string EncryptMessage(string plainText, string aesKeyBase64);
        string DecryptMessage(string cipherText, string aesKeyBase64);

        // 3. operations for safe database storing (Data at rest)
        string EncryptForDatabase(string plainText);
        string DecryptFromDatabase(string cipherText);
    }
}