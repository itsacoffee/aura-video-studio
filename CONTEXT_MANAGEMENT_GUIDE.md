# AI Context Management System

## Overview

The AI Context Management system provides persistent, multi-turn conversation capabilities throughout the video creation workflow. It maintains conversation history, project context, and decision tracking across application sessions.

## Architecture

### Core Components

1. **ConversationContextManager** - Manages conversation message history for each project
2. **ProjectContextManager** - Manages project metadata and AI decision history
3. **ContextPersistence** - Handles JSON-based persistence to local disk
4. **ConversationalLlmService** - Enriches LLM requests with conversation context

### Data Storage

All context data is stored locally in:
- Windows: `%LOCALAPPDATA%\Aura\Conversations` and `%LOCALAPPDATA%\Aura\ProjectContexts`
- Linux/Mac: `~/.local/share/Aura/Conversations` and `~/.local/share/Aura/ProjectContexts`

Files are stored as JSON with atomic write operations for data integrity.

## Data Models

### Message
```typescript
{
  role: "user" | "assistant" | "system",
  content: string,
  timestamp: string,
  metadata?: Record<string, any>
}
```

### ConversationContext
```typescript
{
  projectId: string,
  messages: Message[],
  createdAt: string,
  updatedAt: string,
  metadata?: Record<string, any>
}
```

### VideoMetadata
```typescript
{
  contentType?: string,        // "Tutorial", "Marketing", etc.
  targetPlatform?: string,      // "YouTube", "TikTok", etc.
  audience?: string,            // "Beginners", "Professionals", etc.
  tone?: string,                // "Formal", "Casual", etc.
  durationSeconds?: number,
  keywords?: string[]
}
```

### AiDecision
```typescript
{
  decisionId: string,
  stage: string,                // "script", "visuals", "pacing", etc.
  type: string,                 // "suggestion", "recommendation", etc.
  suggestion: string,
  userAction: "accepted" | "rejected" | "modified",
  userModification?: string,
  timestamp: string
}
```

### ProjectContext
```typescript
{
  projectId: string,
  videoMetadata?: VideoMetadata,
  decisionHistory: AiDecision[],
  createdAt: string,
  updatedAt: string,
  metadata?: Record<string, any>
}
```

## API Endpoints

### POST /api/conversation/{projectId}/message
Send a message with full conversation context.

**Request:**
```json
{
  "message": "How can I improve this script?"
}
```

**Response:**
```json
{
  "success": true,
  "response": "Here are some suggestions...",
  "timestamp": "2025-10-21T12:00:00Z"
}
```

### GET /api/conversation/{projectId}/history
Retrieve conversation history with optional pagination.

**Query Parameters:**
- `maxMessages` (optional): Maximum number of messages to return (default: 100)

**Response:**
```json
{
  "success": true,
  "messages": [...],
  "count": 42
}
```

### DELETE /api/conversation/{projectId}
Clear conversation history for a project.

**Response:**
```json
{
  "success": true,
  "message": "Conversation history cleared"
}
```

### GET /api/conversation/{projectId}/context
Get full project context including metadata and decision history.

**Response:**
```json
{
  "success": true,
  "project": {
    "projectId": "...",
    "videoMetadata": {...},
    "decisionHistory": [...]
  },
  "conversation": {
    "projectId": "...",
    "messages": [...]
  }
}
```

### PUT /api/conversation/{projectId}/context
Update project metadata.

**Request:**
```json
{
  "contentType": "Tutorial",
  "targetPlatform": "YouTube",
  "audience": "Developers",
  "tone": "Professional",
  "durationSeconds": 300,
  "keywords": ["coding", "tutorial"]
}
```

**Response:**
```json
{
  "success": true,
  "message": "Context updated"
}
```

### POST /api/conversation/{projectId}/decision
Record an AI decision and user response.

**Request:**
```json
{
  "stage": "script",
  "type": "suggestion",
  "suggestion": "Add more examples",
  "userAction": "accepted",
  "userModification": null
}
```

**Response:**
```json
{
  "success": true,
  "message": "Decision recorded"
}
```

## Frontend Usage

### Using the ConversationPanel Component

```tsx
import { ConversationPanel } from './components/Conversation';

function MyPage() {
  const projectId = "my-project-123";
  
  return (
    <ConversationPanel 
      projectId={projectId}
      onClose={() => console.log('Panel closed')}
    />
  );
}
```

### Using the Conversation Service

```typescript
import { conversationService } from './services/conversationService';

// Send a message
const response = await conversationService.sendMessage(
  projectId,
  "How can I improve pacing?"
);

// Get conversation history
const history = await conversationService.getHistory(projectId, 50);

// Update project metadata
await conversationService.updateContext(projectId, {
  contentType: "Tutorial",
  targetPlatform: "YouTube",
  audience: "Developers"
});

// Record a decision
await conversationService.recordDecision(projectId, {
  stage: "script",
  type: "suggestion",
  suggestion: "Add introduction",
  userAction: "accepted"
});

// Clear conversation
await conversationService.clearConversation(projectId);
```

## Features

### Context Enrichment
Every message sent to the LLM automatically includes:
- Recent conversation history (last 10 messages)
- Project metadata (content type, platform, audience, etc.)
- Recent decision history (last 5 decisions)

### Multi-Project Isolation
Each project maintains completely separate:
- Conversation histories
- Project metadata
- Decision histories

### Persistence
- Automatic save after each message exchange
- Atomic file operations prevent data corruption
- Survives application restarts
- Thread-safe operations

### User Experience
- Optimistic UI updates for instant feedback
- Auto-scroll to bottom on new messages
- Relative timestamp display (e.g., "5m ago")
- Loading states during AI responses
- Error handling with user-friendly messages
- Keyboard shortcuts (Shift+Enter for new line)

## Development

### Running Tests

```bash
# Run all conversation tests
dotnet test --filter "FullyQualifiedName~Conversation"

# Run specific test categories
dotnet test --filter "FullyQualifiedName~ConversationContextManagerTests"
dotnet test --filter "FullyQualifiedName~ProjectContextManagerTests"
dotnet test --filter "FullyQualifiedName~ContextPersistenceTests"
```

### Adding New Context Types

1. Add fields to `VideoMetadata` model in `ConversationModels.cs`
2. Update `UpdateContextRequest` in `ConversationController.cs`
3. Update frontend types in `conversationService.ts`
4. Update UI in `ConversationPanel.tsx`

### Extending Decision Types

1. Define new stage/type constants
2. Update decision recording logic in `ProjectContextManager`
3. Update frontend to handle new types
4. Add filtering/display logic as needed

## Best Practices

1. **Project IDs**: Use stable, unique identifiers for projects
2. **Message Content**: Keep messages concise but informative
3. **Metadata Updates**: Update project metadata when key parameters change
4. **Decision Recording**: Record all significant AI interactions
5. **Error Handling**: Always handle API errors gracefully
6. **Pagination**: Use `maxMessages` parameter for large conversations

## Security & Privacy

- All data stored locally on user's machine
- No cloud synchronization by default
- File system permissions protect data
- No sensitive data in logs
- API keys stored separately in secure location

## Performance Considerations

- Conversation history limited to 100 messages by default
- Automatic summarization when context exceeds token limits
- Efficient JSON serialization with System.Text.Json
- Thread-safe with minimal locking
- Caching in memory for frequently accessed contexts

## Future Enhancements

- Export conversation history to various formats
- Search within conversation history
- Conversation branching and forking
- Cloud backup/sync (opt-in)
- Advanced summarization with LLM
- Conversation templates
- Multi-user collaboration support
