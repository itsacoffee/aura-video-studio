/**
 * Conversation API service for AI context management
 */

export interface Message {
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: string;
  metadata?: Record<string, any>;
}

export interface VideoMetadata {
  contentType?: string;
  targetPlatform?: string;
  audience?: string;
  tone?: string;
  durationSeconds?: number;
  keywords?: string[];
}

export interface AiDecision {
  decisionId: string;
  stage: string;
  type: string;
  suggestion: string;
  userAction: 'accepted' | 'rejected' | 'modified';
  userModification?: string;
  timestamp: string;
}

export interface ConversationContext {
  projectId: string;
  messages: Message[];
  createdAt: string;
  updatedAt: string;
  metadata?: Record<string, any>;
}

export interface ProjectContext {
  projectId: string;
  videoMetadata?: VideoMetadata;
  decisionHistory: AiDecision[];
  createdAt: string;
  updatedAt: string;
  metadata?: Record<string, any>;
}

export interface SendMessageRequest {
  message: string;
}

export interface SendMessageResponse {
  success: boolean;
  response: string;
  timestamp: string;
}

export interface GetHistoryResponse {
  success: boolean;
  messages: Message[];
  count: number;
}

export interface GetContextResponse {
  success: boolean;
  project: ProjectContext;
  conversation: ConversationContext;
}

export interface UpdateContextRequest {
  contentType?: string;
  targetPlatform?: string;
  audience?: string;
  tone?: string;
  durationSeconds?: number;
  keywords?: string[];
}

export interface RecordDecisionRequest {
  stage: string;
  type: string;
  suggestion: string;
  userAction: 'accepted' | 'rejected' | 'modified';
  userModification?: string;
}

const API_BASE = '/api/conversation';

export const conversationService = {
  /**
   * Send a message with full context
   */
  async sendMessage(projectId: string, message: string): Promise<SendMessageResponse> {
    const response = await fetch(`${API_BASE}/${projectId}/message`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ message } as SendMessageRequest),
    });

    if (!response.ok) {
      throw new Error('Failed to send message');
    }

    return response.json();
  },

  /**
   * Get conversation history with optional pagination
   */
  async getHistory(projectId: string, maxMessages?: number): Promise<GetHistoryResponse> {
    const params = new URLSearchParams();
    if (maxMessages) {
      params.append('maxMessages', maxMessages.toString());
    }

    const response = await fetch(`${API_BASE}/${projectId}/history?${params}`);

    if (!response.ok) {
      throw new Error('Failed to get conversation history');
    }

    return response.json();
  },

  /**
   * Clear conversation context
   */
  async clearConversation(projectId: string): Promise<void> {
    const response = await fetch(`${API_BASE}/${projectId}`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      throw new Error('Failed to clear conversation');
    }
  },

  /**
   * Get full project context
   */
  async getContext(projectId: string): Promise<GetContextResponse> {
    const response = await fetch(`${API_BASE}/${projectId}/context`);

    if (!response.ok) {
      throw new Error('Failed to get context');
    }

    return response.json();
  },

  /**
   * Update project metadata
   */
  async updateContext(projectId: string, metadata: UpdateContextRequest): Promise<void> {
    const response = await fetch(`${API_BASE}/${projectId}/context`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(metadata),
    });

    if (!response.ok) {
      throw new Error('Failed to update context');
    }
  },

  /**
   * Record AI decision and user response
   */
  async recordDecision(projectId: string, decision: RecordDecisionRequest): Promise<void> {
    const response = await fetch(`${API_BASE}/${projectId}/decision`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(decision),
    });

    if (!response.ok) {
      throw new Error('Failed to record decision');
    }
  },

  /**
   * Format timestamp for display
   */
  formatTimestamp(timestamp: string): string {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;

    return date.toLocaleDateString();
  },
};
