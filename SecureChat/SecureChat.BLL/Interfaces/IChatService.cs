using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecureChat.BLL.DTOs.Chat;
using SecureChat.DAL.Entities;

namespace SecureChat.BLL.Interfaces
{
    public interface IChatService
    {
        Task<MessageDto> SendMessageAsync(Guid senderId, Guid receiverId, string encryptedMessageFromClient, string encryptedAesKeyFromClient);

        Task<IEnumerable<MessageDto>> GetChatHistoryAsync(Guid callerId, Guid otherUserId);

        Task<bool> UpdateDeliveryStatusAsync(Guid messageId, DeliveryStatus newStatus);

        // Definition for getting the list of previous contacts
        Task<IEnumerable<UserDto>> GetUserContactsAsync(Guid userId);

        Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId);
    }
}