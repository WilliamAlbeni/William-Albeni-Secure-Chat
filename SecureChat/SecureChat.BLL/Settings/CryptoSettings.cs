namespace SecureChat.BLL.Settings
{
    public class CryptoSettings
    {
        // AES key for encrypting before DB saving (256-bit = 32 bytes)
        public string DbMasterKey { get; set; }

        public string ServerPrivateKey { get; set; }

        public string ServerPublicKey { get; set; }
    }
}