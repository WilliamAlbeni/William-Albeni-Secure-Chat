using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureChat.DAL.Entities
{
    public class Message
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SenderId { get; set; }

        [ForeignKey("SenderId")]
        public User Sender { get; set; }

        [Required]
        public Guid ReceiverId { get; set; }

        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; }

        [Required]
        public string OriginalPayload { get; set; }

        public string? ModifiedPayload { get; set; }

        // for managing MITM attack state
        public MessageState State { get; set; } = MessageState.Pending;

        // for managing UI
        public DeliveryStatus DeliveryStatus { get; set; } = DeliveryStatus.SentToServer;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}