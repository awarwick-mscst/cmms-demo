import React, { useState } from 'react';
import {
  Box,
  Button,
  Divider,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  TextField,
  Typography,
  Menu,
  MenuItem,
} from '@mui/material';
import {
  Add as AddIcon,
  MoreVert as MoreIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { AiConversation } from '../../types';

interface ConversationSidebarProps {
  conversations: AiConversation[];
  selectedId: number | null;
  onSelect: (id: number) => void;
  onCreate: () => void;
  onRename: (id: number, title: string) => void;
  onDelete: (id: number) => void;
}

export const ConversationSidebar: React.FC<ConversationSidebarProps> = ({
  conversations,
  selectedId,
  onSelect,
  onCreate,
  onRename,
  onDelete,
}) => {
  const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);
  const [menuConversation, setMenuConversation] = useState<AiConversation | null>(null);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editTitle, setEditTitle] = useState('');

  const handleMenuOpen = (e: React.MouseEvent<HTMLElement>, conv: AiConversation) => {
    e.stopPropagation();
    setMenuAnchor(e.currentTarget);
    setMenuConversation(conv);
  };

  const handleMenuClose = () => {
    setMenuAnchor(null);
    setMenuConversation(null);
  };

  const handleStartRename = () => {
    if (!menuConversation) return;
    setEditingId(menuConversation.id);
    setEditTitle(menuConversation.title);
    handleMenuClose();
  };

  const handleRename = () => {
    if (editingId && editTitle.trim()) {
      onRename(editingId, editTitle.trim());
    }
    setEditingId(null);
  };

  const handleDelete = () => {
    if (menuConversation) {
      onDelete(menuConversation.id);
    }
    handleMenuClose();
  };

  return (
    <Box
      sx={{
        width: 280,
        borderRight: 1,
        borderColor: 'divider',
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
      }}
    >
      <Box sx={{ p: 2 }}>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          fullWidth
          onClick={onCreate}
        >
          New Chat
        </Button>
      </Box>
      <Divider />
      <List sx={{ flex: 1, overflow: 'auto' }}>
        {conversations.map((conv) => (
          <ListItem
            key={conv.id}
            disablePadding
            secondaryAction={
              <IconButton size="small" onClick={(e) => handleMenuOpen(e, conv)}>
                <MoreIcon fontSize="small" />
              </IconButton>
            }
          >
            {editingId === conv.id ? (
              <Box sx={{ px: 2, py: 1, width: '100%' }}>
                <TextField
                  size="small"
                  fullWidth
                  value={editTitle}
                  onChange={(e) => setEditTitle(e.target.value)}
                  onBlur={handleRename}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') handleRename();
                    if (e.key === 'Escape') setEditingId(null);
                  }}
                  autoFocus
                />
              </Box>
            ) : (
              <ListItemButton
                selected={selectedId === conv.id}
                onClick={() => onSelect(conv.id)}
              >
                <ListItemText
                  primary={conv.title}
                  secondary={new Date(conv.updatedAt || conv.createdAt).toLocaleDateString()}
                  primaryTypographyProps={{ noWrap: true }}
                />
              </ListItemButton>
            )}
          </ListItem>
        ))}
        {conversations.length === 0 && (
          <Typography variant="body2" color="text.secondary" sx={{ p: 2, textAlign: 'center' }}>
            No conversations yet
          </Typography>
        )}
      </List>

      <Menu anchorEl={menuAnchor} open={Boolean(menuAnchor)} onClose={handleMenuClose}>
        <MenuItem onClick={handleStartRename}>
          <EditIcon fontSize="small" sx={{ mr: 1 }} />
          Rename
        </MenuItem>
        <MenuItem onClick={handleDelete} sx={{ color: 'error.main' }}>
          <DeleteIcon fontSize="small" sx={{ mr: 1 }} />
          Delete
        </MenuItem>
      </Menu>
    </Box>
  );
};
