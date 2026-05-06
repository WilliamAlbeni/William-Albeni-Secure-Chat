using System;

namespace SecureChat.BLL.DTOs.Auth
{
    public class AuthResultDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Token { get; set; } // JWT Token (generated in API)
        public string PublicKey { get; set; }
        public string ServerPublicKey { get; set; }
    }
}