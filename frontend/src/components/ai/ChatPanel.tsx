import React, { useEffect, useRef } from 'react';
import { Box, CircularProgress, Typography } from '@mui/material';
import { ChatMessage } from './ChatMessage';
import { ChatInput } from './ChatInput';
import { AiMessage } from '../../types';

interface ChatPanelProps {
  messages: AiMessage[];
  streamingContent: string;
  isStreaming: boolean;
  onSend: (message: string) => void;
  disabled?: boolean;
}

export const ChatPanel: React.FC<ChatPanelProps> = ({
  messages,
  streamingContent,
  isStreaming,
  onSend,
  disabled,
}) => {
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, streamingContent]);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%', flex: 1 }}>
      <Box sx={{ flex: 1, overflow: 'auto', p: 2 }}>
        {messages.length === 0 && !isStreaming && (
          <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
            <Typography variant="body1" color="text.secondary">
              Send a message or use a quick action to get started
            </Typography>
          </Box>
        )}

        {messages.map((msg) => (
          <ChatMessage key={msg.id} message={msg} />
        ))}

        {isStreaming && streamingContent && (
          <ChatMessage
            message={{
              id: -1,
              role: 'assistant',
              content: streamingContent,
              contextType: null,
              createdAt: new Date().toISOString(),
            }}
          />
        )}

        {isStreaming && !streamingContent && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, ml: 5, mb: 2 }}>
            <CircularProgress size={20} />
            <Typography variant="body2" color="text.secondary">
              Thinking...
            </Typography>
          </Box>
        )}

        <div ref={messagesEndRef} />
      </Box>

      <ChatInput onSend={onSend} disabled={disabled || isStreaming} />
    </Box>
  );
};
