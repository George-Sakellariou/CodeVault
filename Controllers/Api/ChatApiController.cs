using Microsoft.AspNetCore.Mvc;
using CodeVault.Models;
using CodeVault.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace CodeVault.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatApiController : ControllerBase
    {
        private readonly ConversationService _conversationService;
        private readonly IOpenAiService _openAiService;
        private readonly CodeService _codeService;

        public ChatApiController(
            ConversationService conversationService,
            IOpenAiService openAiService,
            CodeService codeService)
        {
            _conversationService = conversationService;
            _openAiService = openAiService;
            _codeService = codeService;
        }

        // GET: api/chat/conversations
        [HttpGet("conversations")]
        public async Task<ActionResult<IEnumerable<Conversation>>> GetConversations()
        {
            try
            {
                var conversations = await _conversationService.GetAllAsync();
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/chat/conversations/{id}
        [HttpGet("conversations/{id}")]
        public async Task<ActionResult<Conversation>> GetConversation(int id)
        {
            try
            {
                var conversation = await _conversationService.GetByIdWithMessagesAsync(id);
                if (conversation == null)
                {
                    return NotFound();
                }
                return Ok(conversation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/chat/conversations
        [HttpPost("conversations")]
        public async Task<ActionResult<Conversation>> CreateConversation(string initialMessage)
        {
            try
            {
                var conversation = await _conversationService.CreateAsync(initialMessage);
                return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, conversation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // DELETE: api/chat/conversations/{id}
        [HttpDelete("conversations/{id}")]
        public async Task<IActionResult> DeleteConversation(int id)
        {
            try
            {
                await _conversationService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/chat/message
        [HttpPost("message")]
        public async Task<ActionResult<MessageResponse>> SendMessage(MessageRequest request)
        {
            try
            {
                // Initialize conversation if this is a new chat
                int conversationId = request.ConversationId;
                if (conversationId == 0)
                {
                    var conversation = await _conversationService.CreateAsync(request.Content);
                    conversationId = conversation.Id;
                }

                // Add user message to conversation
                await _conversationService.AddMessageAsync(conversationId, request.Content, true);

                // Get AI response
                var aiResponse = await _openAiService.GetCompletionAsync(request.Content);

                // Add AI response to conversation
                var message = await _conversationService.AddMessageAsync(conversationId, aiResponse, false);

                return Ok(new MessageResponse
                {
                    Success = true,
                    Message = message,
                    ConversationId = conversationId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class MessageRequest
    {
        public string Content { get; set; }
        public int ConversationId { get; set; }
    }

    public class MessageResponse
    {
        public bool Success { get; set; }
        public ChatMessage Message { get; set; }
        public int ConversationId { get; set; }
    }
}