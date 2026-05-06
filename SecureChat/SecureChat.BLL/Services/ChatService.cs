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

        public async Task<MessageDto> SendMessageAsync(Guid senderId, Guid receiverId, string encryptedMessageFromClient, string encryptedAesKeyFromClient)
        {
            // Check users existence
            var sender = await _userRepository.GetUserByIdAsync(senderId);
            var receiver = await _userRepository.GetUserByIdAsync(receiverId);

            if (sender == null || receiver == null)
                throw new Exception("Sender or receiver does not exist in the system");

            string decryptedAesKey = _cryptoService.DecryptAesKeyFromClient(encryptedAesKeyFromClient);

            string plainTextMessage = _cryptoService.DecryptMessage(encryptedMessageFromClient, decryptedAesKey);

            string encryptedPayloadForDb = _cryptoService.EncryptForDatabase(plainTextMessage);

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                OriginalPayload = encryptedPayloadForDb,
                State = MessageState.Delivered,
                DeliveryStatus = DeliveryStatus.SentToServer
            };

            await _messageRepository.AddMessageAsync(message);
            await _messageRepository.SaveChangesAsync();

            // no payload just as an Ack
            return new MessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                DeliveryStatus = message.DeliveryStatus,
                SentAt = message.SentAt
            };
        }

        public async Task<IEnumerable<MessageDto>> GetChatHistoryAsync(Guid callerId, Guid otherUserId)
        {
            var messages = await _messageRepository.GetChatHistoryAsync(callerId, otherUserId);
            var caller = await _userRepository.GetUserByIdAsync(callerId);

            var messageDtos = new List<MessageDto>();

            foreach (var m in messages)
            {
                string plainText = _cryptoService.DecryptFromDatabase(m.OriginalPayload);

                string newSessionAesKey = _cryptoService.GenerateAesKeyBase64();

                string encryptedTextForCaller = _cryptoService.EncryptMessage(plainText, newSessionAesKey);

                string encryptedAesKeyForCaller = _cryptoService.EncryptAesKeyForClient(newSessionAesKey, caller.PublicKey);

                messageDtos.Add(new MessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    EncryptedText = encryptedTextForCaller,
                    EncryptedAesKey = encryptedAesKeyForCaller,
                    DeliveryStatus = m.DeliveryStatus,
                    SentAt = m.SentAt
                });
            }

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