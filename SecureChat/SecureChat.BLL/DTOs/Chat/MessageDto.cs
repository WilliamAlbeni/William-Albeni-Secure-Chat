using System;
using SecureChat.DAL.Entities;

namespace SecureChat.BLL.DTOs.Chat
{
    public class MessageDto
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Text { get; set; } // Plaintext after decrypting
        public DeliveryStatus DeliveryStatus { get; set; }
        public DateTime SentAt { get; set; }
    }
}