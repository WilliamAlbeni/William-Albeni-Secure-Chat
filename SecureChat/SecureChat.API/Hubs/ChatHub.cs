using Microsoft.AspNetCore.SignalR;
using SecureChat.BLL.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace SecureChat.API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        // link UserId with ConnectionId
        private static readonly ConcurrentDictionary<string, string> OnlineUsers = new();

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        // on openning the app: register that user has loged in 
        public async Task RegisterUser(string userId)
        {
            OnlineUsers[userId] = Context.ConnectionId;
            await base.OnConnectedAsync();
        }

        // removing the user when closing the app or internet disconnecting
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var item = OnlineUsers.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId);
            if (item.Key != null)
            {
                OnlineUsers.TryRemove(item.Key, out _);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // sending messages
        public async Task SendPrivateMessage(string senderId, string receiverId, string plainTextMessage)
        {
            // save message in DB
            var messageDto = await _chatService.SendMessageAsync(Guid.Parse(senderId), Guid.Parse(receiverId), plainTextMessage);

            // Checking: is user online now?
            if (OnlineUsers.TryGetValue(receiverId, out string receiverConnectionId))
            {
                // sending message for client 
                await Clients.Client(receiverConnectionId).SendAsync("ReceiveMessage", messageDto);

                // Updating delivery status in DB
                await _chatService.UpdateDeliveryStatusAsync(messageDto.Id, SecureChat.DAL.Entities.DeliveryStatus.DeliveredToDevice);

                // telling the sender that his message has been delivered (two ticks)
                await Clients.Caller.SendAsync("MessageDeliveredToDevice", messageDto.Id);
            }
            else
            {
                // setting the delivery status to: SentToServer (it is by default like that)
            }
        }
        public async Task NotifyMessagesRead(string senderId, string readerId)
        {
            await _chatService.MarkMessagesAsReadAsync(Guid.Parse(senderId), Guid.Parse(readerId));

            if (OnlineUsers.TryGetValue(senderId, out string senderConnectionId))
            {
                await Clients.Client(senderConnectionId).SendAsync("MessagesReadByDevice", readerId);
            }
        }
        // mark message as read when receiver open chat and read the message
        public async Task MarkMessageAsRead(string messageId, string senderId)
        {
            await _chatService.UpdateDeliveryStatusAsync(Guid.Parse(messageId), SecureChat.DAL.Entities.DeliveryStatus.ReadByUser);

            // if sender is online: turn message to read (two blue ticks)
            if (OnlineUsers.TryGetValue(senderId, out string senderConnectionId))
            {
                await Clients.Client(senderConnectionId).SendAsync("MessageReadByUser", messageId);
            }
        }
    }
}