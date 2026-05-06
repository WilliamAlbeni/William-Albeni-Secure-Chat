using Microsoft.AspNetCore.SignalR;
using SecureChat.BLL.DTOs.Chat;
using SecureChat.BLL.Interfaces;
using SecureChat.DAL.Repositories;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace SecureChat.API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly ICryptoService _cryptoService;
        private readonly IUserRepository _userRepository;

        // connect a userId with a connectionId
        private static readonly ConcurrentDictionary<string, string> OnlineUsers = new();

        public ChatHub(IChatService chatService, ICryptoService cryptoService, IUserRepository userRepository)
        {
            _chatService = chatService;
            _cryptoService = cryptoService;
            _userRepository = userRepository;
        }

        public async Task RegisterUser(string userId)
        {
            OnlineUsers[userId] = Context.ConnectionId;
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var item = OnlineUsers.FirstOrDefault(kvp => kvp.Value == Context.ConnectionId);
            if (item.Key != null)
            {
                OnlineUsers.TryRemove(item.Key, out _);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendPrivateMessage(string senderId, string receiverId, string encryptedMessageFromClient, string encryptedAesKeyFromClient)
        {
            var messageDto = await _chatService.SendMessageAsync(
                Guid.Parse(senderId),
                Guid.Parse(receiverId),
                encryptedMessageFromClient,
                encryptedAesKeyFromClient);

            if (OnlineUsers.TryGetValue(receiverId, out string receiverConnectionId))
            {
                string decryptedAesKey = _cryptoService.DecryptAesKeyFromClient(encryptedAesKeyFromClient);
                string plainText = _cryptoService.DecryptMessage(encryptedMessageFromClient, decryptedAesKey);

                var receiver = await _userRepository.GetUserByIdAsync(Guid.Parse(receiverId));

                string newSessionAesKey = _cryptoService.GenerateAesKeyBase64();

                string encryptedTextForReceiver = _cryptoService.EncryptMessage(plainText, newSessionAesKey);
                string encryptedAesKeyForReceiver = _cryptoService.EncryptAesKeyForClient(newSessionAesKey, receiver.PublicKey);

                // DTO for reciepient
                var receiverDto = new MessageDto
                {
                    Id = messageDto.Id,
                    SenderId = messageDto.SenderId,
                    ReceiverId = messageDto.ReceiverId,
                    EncryptedText = encryptedTextForReceiver,
                    EncryptedAesKey = encryptedAesKeyForReceiver,
                    DeliveryStatus = DAL.Entities.DeliveryStatus.DeliveredToDevice,
                    SentAt = messageDto.SentAt
                };

                await Clients.Client(receiverConnectionId).SendAsync("ReceiveMessage", receiverDto);

                await _chatService.UpdateDeliveryStatusAsync(messageDto.Id, SecureChat.DAL.Entities.DeliveryStatus.DeliveredToDevice);

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

        public async Task MarkMessageAsRead(string messageId, string senderId)
        {
            await _chatService.UpdateDeliveryStatusAsync(Guid.Parse(messageId), SecureChat.DAL.Entities.DeliveryStatus.ReadByUser);

            if (OnlineUsers.TryGetValue(senderId, out string senderConnectionId))
            {
                await Clients.Client(senderConnectionId).SendAsync("MessageReadByUser", messageId);
            }
        }
    }
}