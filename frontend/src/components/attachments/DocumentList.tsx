import React from 'react';
import {
  Box,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  ListItemSecondaryAction,
  IconButton,
  Typography,
  Tooltip,
  Paper,
} from '@mui/material';
import {
  PictureAsPdf as PdfIcon,
  Description as DocIcon,
  TableChart as ExcelIcon,
  TextSnippet as TextIcon,
  InsertDriveFile as FileIcon,
  Download as DownloadIcon,
  Delete as DeleteIcon,
} from '@mui/icons-material';
import { Attachment } from '../../types';
import { attachmentService } from '../../services/attachmentService';

interface DocumentListProps {
  documents: Attachment[];
  onDelete?: (id: number) => void;
  canDelete?: boolean;
}

export const DocumentList: React.FC<DocumentListProps> = ({
  documents,
  onDelete,
  canDelete = false,
}) => {
  const getFileIcon = (mimeType: string) => {
    if (mimeType === 'application/pdf') {
      return <PdfIcon color="error" />;
    }
    if (mimeType.includes('word') || mimeType.includes('document')) {
      return <DocIcon color="primary" />;
    }
    if (mimeType.includes('excel') || mimeType.includes('spreadsheet')) {
      return <ExcelIcon color="success" />;
    }
    if (mimeType === 'text/plain') {
      return <TextIcon color="action" />;
    }
    return <FileIcon color="action" />;
  };

  const handleDownload = (attachment: Attachment) => {
    const link = document.createElement('a');
    link.href = attachmentService.getDownloadUrl(attachment.id);
    link.download = attachment.fileName;
    link.click();
  };

  if (documents.length === 0) {
    return null;
  }

  return (
    <Paper variant="outlined">
      <List dense>
        {documents.map((doc, index) => (
          <ListItem
            key={doc.id}
            divider={index < documents.length - 1}
            sx={{
              '&:hover': {
                bgcolor: 'action.hover',
              },
            }}
          >
            <ListItemIcon>{getFileIcon(doc.mimeType)}</ListItemIcon>
            <ListItemText
              primary={
                <Typography
                  variant="body2"
                  sx={{
                    cursor: 'pointer',
                    '&:hover': {
                      textDecoration: 'underline',
                      color: 'primary.main',
                    },
                  }}
                  onClick={() => handleDownload(doc)}
                >
                  {doc.title}
                </Typography>
              }
              secondary={
                <Box component="span" sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
                  <Typography variant="caption" color="text.secondary">
                    {doc.fileName}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {attachmentService.formatFileSize(doc.fileSize)}
                  </Typography>
                  {doc.uploadedByName && (
                    <Typography variant="caption" color="text.secondary">
                      by {doc.uploadedByName}
                    </Typography>
                  )}
                </Box>
              }
            />
            <ListItemSecondaryAction>
              <Tooltip title="Download">
                <IconButton
                  edge="end"
                  onClick={() => handleDownload(doc)}
                  size="small"
                  sx={{ mr: canDelete ? 1 : 0 }}
                >
                  <DownloadIcon />
                </IconButton>
              </Tooltip>
              {canDelete && onDelete && (
                <Tooltip title="Delete">
                  <IconButton
                    edge="end"
                    onClick={() => onDelete(doc.id)}
                    size="small"
                    color="error"
                  >
                    <DeleteIcon />
                  </IconButton>
                </Tooltip>
              )}
            </ListItemSecondaryAction>
          </ListItem>
        ))}
      </List>
    </Paper>
  );
};
