using Microsoft.AspNetCore.Mvc;
using CodeVault.Models;
using CodeVault.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System;

namespace CodeVault.Controllers
{
    public class ChatController : Controller
    {
        private readonly ConversationService _conversationService;
        private readonly IOpenAiService _openAiService;
        private readonly ILogger<ChatController> _logger; // Add this line

        public ChatController(
            ConversationService conversationService,
            IOpenAiService openAiService,
            ILogger<ChatController> logger) // Add this parameter
        {
            _conversationService = conversationService;
            _openAiService = openAiService;
            _logger = logger; // Initialize the logger
        }

        // GET: Chat
        public async Task<IActionResult> Index(int? conversationId = null)
        {
            var conversations = await _conversationService.GetAllAsync();

            if (conversationId.HasValue)
            {
                var currentConversation = await _conversationService.GetByIdWithMessagesAsync(conversationId.Value);
                ViewBag.CurrentConversation = currentConversation;
            }

            return View(conversations);
        }

        // POST: Chat/SendMessage
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            try
            {
                _logger.LogInformation($"SendMessage called with content: {request.Content.Substring(0, Math.Min(50, request.Content.Length))}...");

                // Initialize conversation if this is a new chat
                int conversationId = request.ConversationId;
                _logger.LogInformation($"Conversation ID: {conversationId}");

                if (conversationId == 0)
                {
                    _logger.LogInformation("Creating new conversation");
                    var conversation = await _conversationService.CreateAsync(request.Content);
                    conversationId = conversation.Id;
                    _logger.LogInformation($"New conversation created with ID: {conversationId}");
                }

                // Add user message to conversation
                _logger.LogInformation("Adding user message to conversation");
                await _conversationService.AddMessageAsync(conversationId, request.Content, true);

                // Get AI response
                _logger.LogInformation("Calling OpenAI service");
                var aiResponse = await _openAiService.GetCompletionAsync(request.Content);
                _logger.LogInformation($"OpenAI response received, length: {aiResponse?.Length ?? 0}");

                // Add AI response to conversation
                _logger.LogInformation("Adding AI response to conversation");
                var message = await _conversationService.AddMessageAsync(conversationId, aiResponse, false);

                _logger.LogInformation("SendMessage completed successfully");
                return Json(new
                {
                    success = true,
                    message = message,
                    conversationId = conversationId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessage");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Chat/GetConversations
        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            var conversations = await _conversationService.GetAllAsync();
            return Json(conversations);
        }

        // DELETE: Chat/DeleteConversation
        [HttpDelete]
        public async Task<IActionResult> DeleteConversation(int id)
        {
            try
            {
                await _conversationService.DeleteAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class ChatMessageRequest
    {
        public string Content { get; set; }
        public int ConversationId { get; set; }
    }
}