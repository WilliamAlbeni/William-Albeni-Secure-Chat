using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecureChat.DAL.Entities;

namespace SecureChat.DAL.Repositories
{
    public interface IMessageRepository
    {
        Task AddMessageAsync(Message message);
        Task<Message> GetMessageByIdAsync(Guid id);
        Task<IEnumerable<Message>> GetPendingMessagesAsync(); // for MITM UI
        Task<IEnumerable<Message>> GetChatHistoryAsync(Guid user1Id, Guid user2Id);
        //Task<IEnumerable<User>> GetUserContactsAsync(Guid userId);
        Task<IEnumerable<(User User, int UnreadCount)>> GetUserContactsAsync(Guid userId);
        void UpdateMessage(Message message);
        Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId);
        Task<bool> SaveChangesAsync();
    }
}