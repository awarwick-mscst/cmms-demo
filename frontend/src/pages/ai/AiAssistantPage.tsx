import React, { useState, useCallback, useRef } from 'react';
import { Box, Alert, Typography } from '@mui/material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ConversationSidebar } from '../../components/ai/ConversationSidebar';
import { ChatPanel } from '../../components/ai/ChatPanel';
import { QuickActions } from '../../components/ai/QuickActions';
import { aiService } from '../../services/aiService';
import { AiMessage } from '../../types';

export const AiAssistantPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [selectedConversationId, setSelectedConversationId] = useState<number | null>(null);
  const [messages, setMessages] = useState<AiMessage[]>([]);
  const [streamingContent, setStreamingContent] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const abortRef = useRef<AbortController | null>(null);

  // Check AI status
  const { data: status } = useQuery({
    queryKey: ['ai-status'],
    queryFn: () => aiService.getStatus(),
  });

  // Load conversations
  const { data: conversationsData } = useQuery({
    queryKey: ['ai-conversations'],
    queryFn: () => aiService.getConversations(),
  });

  const conversations = conversationsData?.data || [];

  // Load selected conversation
  const { data: conversationDetail } = useQuery({
    queryKey: ['ai-conversation', selectedConversationId],
    queryFn: () => aiService.getConversation(selectedConversationId!),
    enabled: !!selectedConversationId,
  });

  // Sync messages when conversation loads
  React.useEffect(() => {
    if (conversationDetail?.data) {
      setMessages(conversationDetail.data.messages);
    }
  }, [conversationDetail]);

  // Create conversation
  const createMutation = useMutation({
    mutationFn: (title?: string) => aiService.createConversation(title),
    onSuccess: (data) => {
      if (data.data) {
        setSelectedConversationId(data.data.id);
        setMessages([]);
      }
      queryClient.invalidateQueries({ queryKey: ['ai-conversations'] });
    },
  });

  // Delete conversation
  const deleteMutation = useMutation({
    mutationFn: (id: number) => aiService.deleteConversation(id),
    onSuccess: (_, id) => {
      if (selectedConversationId === id) {
        setSelectedConversationId(null);
        setMessages([]);
      }
      queryClient.invalidateQueries({ queryKey: ['ai-conversations'] });
    },
  });

  // Rename conversation
  const renameMutation = useMutation({
    mutationFn: ({ id, title }: { id: number; title: string }) =>
      aiService.renameConversation(id, title),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-conversations'] });
    },
  });

  // Send message with streaming
  const sendMessage = useCallback(
    async (message: string, contextType?: string, assetId?: number) => {
      let conversationId = selectedConversationId;

      // Auto-create conversation if none selected
      if (!conversationId) {
        const result = await aiService.createConversation(
          message.length > 50 ? message.substring(0, 50) + '...' : message
        );
        if (!result.data) return;
        conversationId = result.data.id;
        setSelectedConversationId(conversationId);
        queryClient.invalidateQueries({ queryKey: ['ai-conversations'] });
      }

      // Add user message optimistically
      const userMessage: AiMessage = {
        id: Date.now(),
        role: 'user',
        content: message,
        contextType: contextType || null,
        createdAt: new Date().toISOString(),
      };
      setMessages((prev) => [...prev, userMessage]);
      setIsStreaming(true);
      setStreamingContent('');

      const abort = new AbortController();
      abortRef.current = abort;

      try {
        let accumulated = '';
        for await (const chunk of aiService.streamMessage(
          conversationId,
          { message, contextType, assetId },
          abort.signal
        )) {
          accumulated += chunk;
          setStreamingContent(accumulated);
        }

        // Replace streaming with final message
        const assistantMessage: AiMessage = {
          id: Date.now() + 1,
          role: 'assistant',
          content: accumulated,
          contextType: contextType || null,
          createdAt: new Date().toISOString(),
        };
        setMessages((prev) => [...prev, assistantMessage]);
        setStreamingContent('');

        // Refresh conversation list for updated timestamps
        queryClient.invalidateQueries({ queryKey: ['ai-conversations'] });
      } catch (err: any) {
        if (err.name !== 'AbortError') {
          const errorMessage: AiMessage = {
            id: Date.now() + 1,
            role: 'assistant',
            content: 'Sorry, I encountered an error. Please try again.',
            contextType: null,
            createdAt: new Date().toISOString(),
          };
          setMessages((prev) => [...prev, errorMessage]);
        }
      } finally {
        setIsStreaming(false);
        abortRef.current = null;
      }
    },
    [selectedConversationId, queryClient]
  );

  const handleQuickAction = useCallback(
    (message: string, contextType: string, assetId?: number) => {
      sendMessage(message, contextType, assetId);
    },
    [sendMessage]
  );

  if (status && !status.enabled) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="info">AI Assistant is not enabled. Contact your administrator.</Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', height: '100%', mx: -3, mt: -3, mb: -3 }}>
      <ConversationSidebar
        conversations={conversations}
        selectedId={selectedConversationId}
        onSelect={(id) => {
          setSelectedConversationId(id);
          setStreamingContent('');
        }}
        onCreate={() => createMutation.mutate(undefined)}
        onRename={(id, title) => renameMutation.mutate({ id, title })}
        onDelete={(id) => deleteMutation.mutate(id)}
      />

      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', height: '100%' }}>
        {!selectedConversationId && messages.length === 0 ? (
          <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
            <Typography variant="h5" textAlign="center" gutterBottom>
              AI Maintenance Assistant
            </Typography>
            <Typography variant="body1" color="text.secondary" textAlign="center" sx={{ mb: 3 }}>
              Ask a question or choose a quick action below
            </Typography>
            <QuickActions onAction={handleQuickAction} disabled={isStreaming} />
          </Box>
        ) : (
          <ChatPanel
            messages={messages}
            streamingContent={streamingContent}
            isStreaming={isStreaming}
            onSend={(msg) => sendMessage(msg)}
            disabled={!selectedConversationId && !isStreaming}
          />
        )}
      </Box>
    </Box>
  );
};
