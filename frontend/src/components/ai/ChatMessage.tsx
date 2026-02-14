import React from 'react';
import { Box, Paper, Typography, Chip } from '@mui/material';
import { SmartToy as AiIcon, Person as PersonIcon } from '@mui/icons-material';
import ReactMarkdown from 'react-markdown';
import { AiMessage } from '../../types';

interface ChatMessageProps {
  message: AiMessage;
}

const contextLabels: Record<string, string> = {
  predictive: 'Predictive Analysis',
  downtime_followup: 'Down Machines',
  overdue: 'Overdue Maintenance',
  asset_health: 'Asset Health',
};

export const ChatMessage: React.FC<ChatMessageProps> = ({ message }) => {
  const isUser = message.role === 'user';

  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: isUser ? 'flex-end' : 'flex-start',
        mb: 2,
      }}
    >
      <Box sx={{ display: 'flex', gap: 1, maxWidth: '80%', alignItems: 'flex-start' }}>
        {!isUser && (
          <AiIcon
            sx={{
              mt: 1,
              color: 'primary.main',
              fontSize: 28,
            }}
          />
        )}
        <Paper
          elevation={1}
          sx={{
            p: 2,
            bgcolor: isUser ? 'primary.main' : 'background.paper',
            color: isUser ? 'primary.contrastText' : 'text.primary',
            borderRadius: 2,
          }}
        >
          {message.contextType && contextLabels[message.contextType] && (
            <Chip
              label={contextLabels[message.contextType]}
              size="small"
              color="secondary"
              sx={{ mb: 1 }}
            />
          )}
          <Box
            sx={{
              '& p': { m: 0, mb: 1 },
              '& p:last-child': { mb: 0 },
              '& pre': {
                bgcolor: isUser ? 'rgba(0,0,0,0.2)' : 'action.hover',
                p: 1,
                borderRadius: 1,
                overflow: 'auto',
              },
              '& code': {
                fontFamily: 'monospace',
                fontSize: '0.875rem',
              },
              '& ul, & ol': { pl: 2, mb: 1 },
              '& li': { mb: 0.5 },
            }}
          >
            <ReactMarkdown>{message.content}</ReactMarkdown>
          </Box>
          <Typography variant="caption" sx={{ opacity: 0.7, display: 'block', mt: 0.5 }}>
            {new Date(message.createdAt).toLocaleTimeString()}
          </Typography>
        </Paper>
        {isUser && (
          <PersonIcon
            sx={{
              mt: 1,
              color: 'text.secondary',
              fontSize: 28,
            }}
          />
        )}
      </Box>
    </Box>
  );
};
