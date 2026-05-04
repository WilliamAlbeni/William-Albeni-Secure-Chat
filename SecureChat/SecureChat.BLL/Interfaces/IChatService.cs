using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecureChat.BLL.DTOs.Chat;
using SecureChat.DAL.Entities;

namespace SecureChat.BLL.Interfaces
{
    public interface IChatService
    {
        Task<MessageDto> SendMessageAsync(Guid senderId, Guid receiverId, string plainTextMessage);

        Task<IEnumerable<MessageDto>> GetChatHistoryAsync(Guid user1Id, Guid user2Id);

        Task<bool> UpdateDeliveryStatusAsync(Guid messageId, DeliveryStatus newStatus);

        // Definition for getting the list of previous contacts
        Task<IEnumerable<UserDto>> GetUserContactsAsync(Guid userId);

        Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId);
    }
}