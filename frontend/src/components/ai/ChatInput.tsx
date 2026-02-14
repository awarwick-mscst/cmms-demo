import React, { useState, useRef } from 'react';
import { Box, TextField, IconButton } from '@mui/material';
import { Send as SendIcon } from '@mui/icons-material';

interface ChatInputProps {
  onSend: (message: string) => void;
  disabled?: boolean;
}

export const ChatInput: React.FC<ChatInputProps> = ({ onSend, disabled }) => {
  const [message, setMessage] = useState('');
  const inputRef = useRef<HTMLTextAreaElement>(null);

  const handleSend = () => {
    const trimmed = message.trim();
    if (!trimmed || disabled) return;
    onSend(trimmed);
    setMessage('');
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <Box sx={{ display: 'flex', gap: 1, p: 2, borderTop: 1, borderColor: 'divider' }}>
      <TextField
        inputRef={inputRef}
        fullWidth
        multiline
        maxRows={4}
        placeholder="Ask a maintenance question..."
        value={message}
        onChange={(e) => setMessage(e.target.value)}
        onKeyDown={handleKeyDown}
        disabled={disabled}
        size="small"
      />
      <IconButton
        color="primary"
        onClick={handleSend}
        disabled={disabled || !message.trim()}
        sx={{ alignSelf: 'flex-end' }}
      >
        <SendIcon />
      </IconButton>
    </Box>
  );
};
