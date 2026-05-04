using System;

namespace SecureChat.BLL.DTOs.Chat
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string? PublicKey { get; set; }
        public int UnreadCount { get; set; }
    }
}