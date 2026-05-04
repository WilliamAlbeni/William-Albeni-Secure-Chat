using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SecureChat.DAL.Data;
using SecureChat.DAL.Entities;

namespace SecureChat.DAL.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ApplicationDbContext _context;

        public MessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddMessageAsync(Message message)
        {
            await _context.Messages.AddAsync(message);
        }

        public async Task<Message> GetMessageByIdAsync(Guid id)
        {
            return await _context.Messages.FindAsync(id);
        }

        // the attacker will use this function to get the pending messages to modify it
        public async Task<IEnumerable<Message>> GetPendingMessagesAsync()
        {
            return await _context.Messages
                .Where(m => m.State == MessageState.Pending)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        // this function will bring the chat between 2 users
        public async Task<IEnumerable<Message>> GetChatHistoryAsync(Guid user1Id, Guid user2Id)
        {
            return await _context.Messages
                .Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                            (m.SenderId == user2Id && m.ReceiverId == user1Id))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<(User User, int UnreadCount)>> GetUserContactsAsync(Guid userId)
        {
            var myMessages = await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .ToListAsync();

            var contactIds = myMessages
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToList();

            var queryResult = await _context.Users
                .Where(u => contactIds.Contains(u.Id))
                .Select(u => new
                {
                    User = u,
                    Count = _context.Messages.Count(m => m.SenderId == u.Id && m.ReceiverId == userId && (int)m.DeliveryStatus != 3)
                })
                .ToListAsync();

            return queryResult.Select(r => (r.User, r.Count));
        }

        public async Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId)
        {
            // Find all unread messages from the sender to me
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && (int)m.DeliveryStatus != 3)
                .ToListAsync();

            // Update their status to 3 (Read)
            foreach (var msg in unreadMessages)
            {
                // Cast the integer 3 to your Enum type (assuming it's called DeliveryStatus)
                msg.DeliveryStatus = (DeliveryStatus)3;
            }

            await _context.SaveChangesAsync();
        }

        public void UpdateMessage(Message message)
        {
            _context.Messages.Update(message);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}