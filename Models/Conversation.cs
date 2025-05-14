using System;
using System.Collections.Generic;

namespace CodeVault.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}