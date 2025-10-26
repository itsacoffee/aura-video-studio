/**
 * Conversation API service for AI context management
 */

import { get, post, put, del } from './api/apiClient';

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
    return post<SendMessageResponse>(`${API_BASE}/${projectId}/message`, {
      message,
    } as SendMessageRequest);
  },

  /**
   * Get conversation history with optional pagination
   */
  async getHistory(projectId: string, maxMessages?: number): Promise<GetHistoryResponse> {
    const params = new URLSearchParams();
    if (maxMessages) {
      params.append('maxMessages', maxMessages.toString());
    }

    return get<GetHistoryResponse>(`${API_BASE}/${projectId}/history?${params}`);
  },

  /**
   * Clear conversation context
   */
  async clearConversation(projectId: string): Promise<void> {
    return del<void>(`${API_BASE}/${projectId}`);
  },

  /**
   * Get full project context
   */
  async getContext(projectId: string): Promise<GetContextResponse> {
    return get<GetContextResponse>(`${API_BASE}/${projectId}/context`);
  },

  /**
   * Update project metadata
   */
  async updateContext(projectId: string, metadata: UpdateContextRequest): Promise<void> {
    return put<void>(`${API_BASE}/${projectId}/context`, metadata);
  },

  /**
   * Record AI decision and user response
   */
  async recordDecision(projectId: string, decision: RecordDecisionRequest): Promise<void> {
    return post<void>(`${API_BASE}/${projectId}/decision`, decision);
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
