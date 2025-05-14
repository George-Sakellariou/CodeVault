using System;

namespace CodeVault.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public required string Content { get; set; }
        public bool IsFromUser { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int ConversationId { get; set; }
        public Conversation? Conversation { get; set; }
    }
}