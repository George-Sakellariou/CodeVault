using CodeVault.Data;
using CodeVault.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeVault.Services
{
    public class ConversationService
    {
        private readonly CodeDbContext _context;

        public ConversationService(CodeDbContext context)
        {
            _context = context;
        }

        // Get all conversations
        public async Task<List<Conversation>> GetAllAsync()
        {
            return await _context.Conversations
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        // Get conversation by ID with messages
        public async Task<Conversation> GetByIdWithMessagesAsync(int id)
        {
            return await _context.Conversations
                .Include(c => c.Messages.OrderBy(m => m.Timestamp))
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // Create new conversation
        public async Task<Conversation> CreateAsync(string initialMessage)
        {
            // Generate a title based on the initial message
            string title = GenerateTitle(initialMessage);

            var conversation = new Conversation
            {
                Title = title,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return conversation;
        }

        // Add message to conversation
        public async Task<ChatMessage> AddMessageAsync(int conversationId, string content, bool isFromUser)
        {
            var conversation = await _context.Conversations.FindAsync(conversationId);

            if (conversation == null)
                throw new KeyNotFoundException($"Conversation with ID {conversationId} not found.");

            // Update the conversation's UpdatedAt timestamp
            conversation.UpdatedAt = DateTime.UtcNow;
            _context.Conversations.Update(conversation);

            // Create and add the new message
            var message = new ChatMessage
            {
                Content = content,
                IsFromUser = isFromUser,
                Timestamp = DateTime.UtcNow,
                ConversationId = conversationId
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return message;
        }

        // Delete conversation
        public async Task DeleteAsync(int id)
        {
            var conversation = await _context.Conversations.FindAsync(id);

            if (conversation == null)
                throw new KeyNotFoundException($"Conversation with ID {id} not found.");

            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();
        }

        // Generate a title based on the initial message
        private string GenerateTitle(string initialMessage)
        {
            // Simple implementation: Take first 30 chars of the message or up to the first period
            int endIndex = initialMessage.Length > 30 ? 30 : initialMessage.Length;
            int periodIndex = initialMessage.IndexOf('.');

            if (periodIndex > 0 && periodIndex < endIndex)
                endIndex = periodIndex;

            string title = initialMessage.Substring(0, endIndex).Trim();

            // Add ellipsis if we truncated the message
            if (endIndex < initialMessage.Length && !title.EndsWith("..."))
                title += "...";

            return title;
        }
    }
}