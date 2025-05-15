// Auto-resize textarea
function autoResizeTextarea(textarea) {
    textarea.style.height = 'auto';
    textarea.style.height = (textarea.scrollHeight) + 'px';
    
    if (textarea.scrollHeight > 150) {
        textarea.style.overflowY = 'auto';
        textarea.style.height = '150px';
    } else {
        textarea.style.overflowY = 'hidden';
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Auto-resize message input
    const messageInput = document.getElementById('messageInput');
    if (messageInput) {
        messageInput.addEventListener('input', function() {
            autoResizeTextarea(this);
        });
    }
    
    // Initialize syntax highlighting
    document.querySelectorAll('pre code').forEach((block) => {
        if (typeof hljs !== 'undefined') {
            hljs.highlightBlock(block);
        }
    });
    
    // Initialize tooltips
    if (typeof bootstrap !== 'undefined') {
        const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
        const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl));
    }
    
    // Send message on button click 
    const sendButton = document.getElementById('sendButton');
    if (sendButton) {
        sendButton.addEventListener('click', sendMessage);
    }
    
    // Send message on Enter key (not Shift+Enter)
    if (messageInput) {
        messageInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });
    }
    
    // Delete conversation button handling
    document.querySelectorAll('.delete-conversation-btn').forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            const conversationId = this.getAttribute('data-id');
            document.getElementById('confirmDelete').setAttribute('data-id', conversationId);
            
            // Show modal via Bootstrap
            const modal = new bootstrap.Modal(document.getElementById('deleteModal'));
            modal.show();
        });
    });
    
    // Confirm delete button
    const confirmDeleteBtn = document.getElementById('confirmDelete');
    if (confirmDeleteBtn) {
        confirmDeleteBtn.addEventListener('click', function() {
            const conversationId = this.getAttribute('data-id');
            
            fetch('/Chat/DeleteConversation?id=' + conversationId, {
                method: 'DELETE',
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    // Hide modal
                    const modal = bootstrap.Modal.getInstance(document.getElementById('deleteModal'));
                    modal.hide();
                    
                    // Reload page or remove conversation from list
                    location.reload();
                } else {
                    alert('Failed to delete conversation: ' + data.message);
                }
            })
            .catch(error => {
                alert('Failed to delete conversation. Please try again.');
            });
        });
    }
    
    // Mobile sidebar toggle
    const mobileSidebarToggle = document.getElementById('mobileSidebarToggle');
    if (mobileSidebarToggle) {
        mobileSidebarToggle.addEventListener('click', function() {
            document.getElementById('mobileSidebar').classList.add('active');
            document.getElementById('sidebarOverlay').classList.add('active');
            document.body.classList.add('sidebar-open');
        });
    }
    
    // Close sidebar
    const closeSidebar = document.getElementById('closeSidebar');
    const sidebarOverlay = document.getElementById('sidebarOverlay');
    if (closeSidebar && sidebarOverlay) {
        closeSidebar.addEventListener('click', closeMobileSidebar);
        sidebarOverlay.addEventListener('click', closeMobileSidebar);
    }
    
    function closeMobileSidebar() {
        document.getElementById('mobileSidebar').classList.remove('active');
        document.getElementById('sidebarOverlay').classList.remove('active');
        document.body.classList.remove('sidebar-open');
    }
});

// Send message function
function sendMessage() {
    const messageInput = document.getElementById('messageInput');
    const message = messageInput.value.trim();
    if (message === "") return;
    
    // Get current conversation ID
    let conversationId = 0;
    
    // Try to find conversation ID in the DOM
    const currentConversation = document.querySelector('.conversation-item.active');
    if (currentConversation) {
        conversationId = currentConversation.getAttribute('data-id');
    }
    
    // Clear welcome message if present
    const welcomeMessage = document.getElementById('welcome-message');
    if (welcomeMessage) {
        welcomeMessage.style.display = 'none';
    }
    
    // Add user message to chat
    addMessageToChat(message, true);
    messageInput.value = "";
    messageInput.style.height = 'auto'; // Reset textarea height
    
    // Add typing indicator
    const chatContainer = document.getElementById('chatContainer');
    const typingIndicator = document.createElement('div');
    typingIndicator.className = 'message mb-2 p-3 rounded bg-light typing-indicator';
    typingIndicator.innerHTML = `
        <div class="d-flex align-items-center">
            <div class="avatar bg-primary rounded-circle d-flex justify-content-center align-items-center me-2">
                <i class="bi bi-code-square text-white"></i>
            </div>
            <div class="typing-dots">
                <span></span>
                <span></span>
                <span></span>
            </div>
        </div>
    `;
    
    if (chatContainer) {
        chatContainer.appendChild(typingIndicator);
        scrollToBottom();
    }
    
    // Send to server
    fetch('/Chat/SendMessage', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            content: message,
            conversationId: conversationId
        })
    })
    .then(response => response.json())
    .then(data => {
        // Remove typing indicator
        const typingIndicators = document.querySelectorAll('.typing-indicator');
        typingIndicators.forEach(indicator => indicator.remove());
        
        if (data.success) {
            // Add AI response
            addMessageToChat(data.message.content, false);
            
            // Update the current conversation ID if this is a new conversation
            if (conversationId === 0 && data.conversationId) {
                // Refresh conversation list
                refreshConversationList();
            }
        } else {
            addMessageToChat("Sorry, I couldn't process your request. Please try again.", false);
        }
    })
    .catch(error => {
        // Remove typing indicator
        const typingIndicators = document.querySelectorAll('.typing-indicator');
        typingIndicators.forEach(indicator => indicator.remove());
        
        addMessageToChat("Sorry, I couldn't process your request. Please try again.", false);
    });
}

// Add message to the chat
function addMessageToChat(content, isFromUser) {
    const timestamp = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    let messageDiv = document.createElement('div');
    
    if (isFromUser) {
        messageDiv.className = 'message user-message mb-3 p-0';
        messageDiv.innerHTML = `
            <div class="d-flex justify-content-end">
                <div class="message-content p-3 rounded-3 bg-primary text-white max-w-75">
                    ${escapeHtml(content)}
                    <div class="message-time text-end mt-1">
                        <small class="opacity-75">${timestamp}</small>
                    </div>
                </div>
            </div>
        `;
    } else {
        // Process markdown-like formatting in AI responses
        const processedContent = processContent(content);
        
        messageDiv.className = 'message ai-message mb-3 p-0';
        messageDiv.innerHTML = `
            <div class="d-flex">
                <div class="avatar bg-primary rounded-circle d-flex justify-content-center align-items-center me-2" style="width: 38px; height: 38px; min-width: 38px;">
                    <i class="bi bi-code-square text-white"></i>
                </div>
                <div class="message-content p-3 rounded-3 bg-light max-w-75">
                    ${processedContent}
                    <div class="message-time text-end mt-1">
                        <small class="text-muted">${timestamp}</small>
                    </div>
                </div>
            </div>
        `;
    }
    
    const chatContainer = document.getElementById('chatContainer');
    if (chatContainer) {
        chatContainer.appendChild(messageDiv);
        
        // Apply syntax highlighting
        if (!isFromUser) {
            messageDiv.querySelectorAll('pre code').forEach((block) => {
                if (typeof hljs !== 'undefined') {
                    hljs.highlightBlock(block);
                }
            });
        }
        
        scrollToBottom();
    }
}

// Scroll to bottom of chat container
function scrollToBottom() {
    const chatContainer = document.getElementById('chatContainer');
    if (chatContainer) {
        chatContainer.scrollTop = chatContainer.scrollHeight;
    }
}

// Process content for better formatting
function processContent(content) {
    // Replace ** for bold
    content = content.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    
    // Replace * or _ for italic
    content = content.replace(/(\*|_)(.*?)(\*|_)/g, '<em>$2</em>');
    
    // Replace ``` for code blocks with language detection
    content = content.replace(/```([a-zA-Z0-9]+)?\n([\s\S]*?)```/g, function(match, language, code) {
        const lang = language ? ` class="language-${language}"` : '';
        return `<pre><code${lang}>${code}</code></pre>`;
    });
    
    // Replace ` for inline code
    content = content.replace(/`(.*?)`/g, '<code class="bg-light px-1 rounded">$1</code>');
    
    // Replace URLs with links
    content = content.replace(/(https?:\/\/[^\s]+)/g, '<a href="$1" target="_blank">$1</a>');
    
    // Replace line breaks with <br>
    content = content.replace(/\n/g, '<br>');
    
    return content;
}

// Escape HTML to prevent XSS in user messages
function escapeHtml(unsafe) {
    return unsafe
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;")
        .replace(/\n/g, '<br>');
}

// Refresh conversation list
function refreshConversationList() {
    fetch('/Chat/GetConversations')
        .then(response => response.json())
        .then(data => {
            updateConversationList(data);
        })
        .catch(error => {
            console.error('Error fetching conversations:', error);
        });
}

// Update conversation list in sidebar
function updateConversationList(conversations) {
    const listEl = document.getElementById('conversationList');
    const mobileListEl = document.getElementById('mobileSidebarContent');
    
    if (!listEl || !mobileListEl) return;
    
    // Clear current list
    listEl.innerHTML = '';
    mobileListEl.innerHTML = '';
    
    if (conversations && conversations.length > 0) {
        const listItems = conversations.map(conversation => {
            const formattedDate = new Date(conversation.updatedAt).toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric',
                year: 'numeric',
                hour: 'numeric',
                minute: 'numeric',
                hour12: true
            });
            
            return `
                <a href="/Chat/Index?conversationId=${conversation.id}"
                   class="list-group-item list-group-item-action border-0 d-flex justify-content-between align-items-center conversation-item${conversation.id === window.currentConversationId ? ' active' : ''}"
                   data-id="${conversation.id}">
                    <div class="conversation-info">
                        <div class="d-flex align-items-center">
                            <i class="bi bi-chat-text me-2"></i>
                            <div class="text-truncate conversation-title">${conversation.title}</div>
                        </div>
                        <small class="text-muted">
                            ${formattedDate}
                        </small>
                    </div>
                    <button class="btn btn-sm btn-outline-danger delete-conversation-btn"
                            data-id="${conversation.id}" title="Delete conversation">
                        <i class="bi bi-trash"></i>
                    </button>
                </a>
            `;
        }).join('');
        
        listEl.innerHTML = listItems;
        mobileListEl.innerHTML = listItems;
        
        // Reattach event listeners
        document.querySelectorAll('.delete-conversation-btn').forEach(button => {
            button.addEventListener('click', function(e) {
                e.preventDefault();
                e.stopPropagation();
                const conversationId = this.getAttribute('data-id');
                document.getElementById('confirmDelete').setAttribute('data-id', conversationId);
                
                // Show modal via Bootstrap
                const modal = new bootstrap.Modal(document.getElementById('deleteModal'));
                modal.show();
            });
        });
    } else {
        const emptyMessage = `
            <div class="p-4 text-center text-muted">
                <i class="bi bi-chat-square-text mb-2" style="font-size: 2rem;"></i>
                <p>No conversations yet</p>
                <p>Start a new chat to begin</p>
            </div>
        `;
        
        listEl.innerHTML = emptyMessage;
        mobileListEl.innerHTML = emptyMessage;
    }
}
