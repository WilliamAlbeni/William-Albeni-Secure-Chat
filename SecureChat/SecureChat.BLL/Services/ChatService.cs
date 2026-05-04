using Microsoft.EntityFrameworkCore;
using SecureChat.BLL.DTOs.Chat;
using SecureChat.BLL.Interfaces;
using SecureChat.DAL.Entities;
using SecureChat.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecureChat.BLL.Services
{
    public class ChatService : IChatService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICryptoService _cryptoService;

        public ChatService(IMessageRepository messageRepository, IUserRepository userRepository, ICryptoService cryptoService)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _cryptoService = cryptoService;
        }

        public async Task<MessageDto> SendMessageAsync(Guid senderId, Guid receiverId, string plainTextMessage)
        {
            // 1. Checking of users existence
            var sender = await _userRepository.GetUserByIdAsync(senderId);
            var receiver = await _userRepository.GetUserByIdAsync(receiverId);

            if (sender == null || receiver == null)
                throw new Exception("Sender or receiver does not exist in the system");

            // 2. encrypt before storing
            string encryptedPayload = _cryptoService.EncryptForDatabase(plainTextMessage);

            // 3. Create object
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                OriginalPayload = encryptedPayload,

                State = MessageState.Delivered,

                DeliveryStatus = DeliveryStatus.SentToServer
            };

            // 4. Storing in DB
            await _messageRepository.AddMessageAsync(message);
            await _messageRepository.SaveChangesAsync();

            // 5. returning to UI as DTO
            return new MessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Text = plainTextMessage,
                DeliveryStatus = message.DeliveryStatus,
                SentAt = message.SentAt
            };
        }

        public async Task<IEnumerable<MessageDto>> GetChatHistoryAsync(Guid user1Id, Guid user2Id)
        { 
            var messages = await _messageRepository.GetChatHistoryAsync(user1Id, user2Id);

            // decrypt and convert to DTOs
            var messageDtos = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,

                Text = _cryptoService.DecryptFromDatabase(m.OriginalPayload),
                DeliveryStatus = m.DeliveryStatus,
                SentAt = m.SentAt
            }).ToList();

            return messageDtos;
        }

        public async Task<bool> UpdateDeliveryStatusAsync(Guid messageId, DeliveryStatus newStatus)
        {
            // 1. Search for message
            var message = await _messageRepository.GetMessageByIdAsync(messageId);
            if (message == null) return false;

            // 2. Change status and save
            message.DeliveryStatus = newStatus;
            _messageRepository.UpdateMessage(message);

            return await _messageRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<UserDto>> GetUserContactsAsync(Guid userId)
        {
            var contactsData = await _messageRepository.GetUserContactsAsync(userId);

            return contactsData.Select(c => new UserDto
            {
                Id = c.User.Id,
                Username = c.User.Username,
                PublicKey = c.User.PublicKey,
                UnreadCount = c.UnreadCount
            });
        }

        public async Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId)
        {
            // Pass the request to the repository
            await _messageRepository.MarkMessagesAsReadAsync(senderId, receiverId);
        }
    }
}