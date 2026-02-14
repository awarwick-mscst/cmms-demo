import api from './api';
import { store } from '../store';
import {
  AiConversation,
  AiConversationDetail,
  AiMessage,
  AiStatus,
  ApiResponse,
  SendMessageRequest,
} from '../types';

// Determine base URL for streaming (fetch, not axios)
const getApiBaseUrl = (): string => {
  if (process.env.REACT_APP_API_URL) {
    return process.env.REACT_APP_API_URL;
  }
  const { protocol, hostname, port } = window.location;
  if (protocol === 'https:' && (!port || port === '443')) {
    return `${protocol}//${hostname}/api/v1`;
  }
  const apiPort = protocol === 'https:' ? 5001 : 5000;
  return `${protocol}//${hostname}:${apiPort}/api/v1`;
};

export const aiService = {
  getStatus: async (): Promise<AiStatus> => {
    const response = await api.get<AiStatus>('/ai/status');
    return response.data;
  },

  getConversations: async (): Promise<ApiResponse<AiConversation[]>> => {
    const response = await api.get<ApiResponse<AiConversation[]>>('/ai/conversations');
    return response.data;
  },

  createConversation: async (title?: string): Promise<ApiResponse<AiConversation>> => {
    const response = await api.post<ApiResponse<AiConversation>>('/ai/conversations', { title });
    return response.data;
  },

  getConversation: async (id: number): Promise<ApiResponse<AiConversationDetail>> => {
    const response = await api.get<ApiResponse<AiConversationDetail>>(`/ai/conversations/${id}`);
    return response.data;
  },

  deleteConversation: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/ai/conversations/${id}`);
    return response.data;
  },

  renameConversation: async (id: number, title: string): Promise<ApiResponse<void>> => {
    const response = await api.put<ApiResponse<void>>(`/ai/conversations/${id}/title`, { title });
    return response.data;
  },

  sendMessage: async (conversationId: number, request: SendMessageRequest): Promise<ApiResponse<AiMessage>> => {
    const response = await api.post<ApiResponse<AiMessage>>(`/ai/conversations/${conversationId}/messages`, request);
    return response.data;
  },

  streamMessage: async function* (
    conversationId: number,
    request: SendMessageRequest,
    signal?: AbortSignal
  ): AsyncGenerator<string> {
    const baseUrl = getApiBaseUrl();
    const token = store.getState().auth.accessToken;

    const response = await fetch(`${baseUrl}/ai/conversations/${conversationId}/stream`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(request),
      signal,
    });

    if (!response.ok) {
      throw new Error(`Stream request failed: ${response.status}`);
    }

    const reader = response.body?.getReader();
    if (!reader) throw new Error('No response body');

    const decoder = new TextDecoder();
    let buffer = '';

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n');
      buffer = lines.pop() || '';

      for (const line of lines) {
        if (!line.startsWith('data: ')) continue;
        const data = line.slice(6);
        if (data === '[DONE]') return;

        try {
          const parsed = JSON.parse(data);
          if (parsed.error) throw new Error(parsed.error);
          if (parsed.content) yield parsed.content;
        } catch (e) {
          if (e instanceof SyntaxError) continue;
          throw e;
        }
      }
    }
  },
};
