using Microsoft.AspNetCore.Mvc;
using SecureChat.BLL.Interfaces;
using System;
using System.Threading.Tasks;

namespace SecureChat.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] Guid user1Id, [FromQuery] Guid user2Id)
        {
            try
            {
                var messages = await _chatService.GetChatHistoryAsync(user1Id, user2Id);

                return Ok(messages);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to load history", error = ex.Message });
            }
        }

        // GET: api/Chat/contacts?userId=...
        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts([FromQuery] Guid userId)
        {
            try
            {
                var contacts = await _chatService.GetUserContactsAsync(userId);
                return Ok(contacts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to load contacts", error = ex.Message });
            }
        }

        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkMessagesAsRead([FromQuery] Guid senderId, [FromQuery] Guid myId)
        {
            try
            {
                await _chatService.MarkMessagesAsReadAsync(senderId, myId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error updating read status", error = ex.Message });
            }
        }
    }
}