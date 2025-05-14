using Microsoft.AspNetCore.Mvc;
using CodeVault.Models;
using CodeVault.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace CodeVault.Controllers
{
    public class ChatController : Controller
    {
        private readonly ConversationService _conversationService;
        private readonly IOpenAiService _openAiService;

        public ChatController(
            ConversationService conversationService,
            IOpenAiService openAiService)
        {
            _conversationService = conversationService;
            _openAiService = openAiService;
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

                return Json(new
                {
                    success = true,
                    message = message,
                    conversationId = conversationId
                });
            }
            catch (Exception ex)
            {
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